# Transfer Asset Flow

> Direct asset transfer between two internal user wallets, verifying idempotency, ownership, and available balance, executing atomic balance adjustments, creating transaction audit records, and notifying both parties.

---

## Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    %% Last verified against commit: b5229aa

    actor Sender as Sender Client
    participant Gateway as API.Gateway (Ocelot)
    participant Middleware as IdempotencyMiddleware (Portfolio.API)
    participant Redis as Redis Cache (Portfolio.API)
    participant Controller as WalletController (Portfolio.API)
    participant WalletSvc as WalletService (Portfolio.API)
    participant WalletRepo as WalletRepository (PortfolioDb)
    participant AssetRepo as AssetRepository (PortfolioDb)
    participant TxSvc as TransactionService (PortfolioDb)
    participant Bus as RabbitMQ (MassTransit)
    participant NConsumer as AssetTransferConsumer (Notifications.API)
    participant NotifSvc as NotificationService (Notifications.API)
    participant NHub as NotificationHub (SignalR)
    actor Recipient as Recipient Client

    Sender->>Gateway: POST /api/wallet/assets/{symbol}/transfers (Bearer JWT, Idempotency-Key, TransferAssetRequest)
    Gateway->>Middleware: Forward HTTP request

    alt Missing Idempotency-Key header
        Middleware-->>Gateway: 400 Bad Request ("Idempotency-Key header is mandotory")
        Gateway-->>Sender: 400 Bad Request
    else Idempotency-Key provided
        Middleware->>Redis: GetAsync<string>("Idemp_{idempotencyKey}")
        alt Key already exists (Duplicate request)
            Redis-->>Middleware: Existing key found
            Middleware-->>Gateway: 409 Conflict ("This transaction is either pending or already done")
            Gateway-->>Sender: 409 Conflict
        else Fresh idempotency key
            Redis-->>Middleware: null
            Middleware->>Redis: SetAsync("Idemp_{idempotencyKey}", key, 24h TTL)
            Middleware->>Controller: Invoke next(context)

            Controller->>Controller: User.FindFirstValue(ClaimTypes.NameIdentifier)
            Controller->>WalletSvc: TransferAsset(TransferAssetDto { UserId, Symbol, AssetAmount, TargetWalletAddress })

            WalletSvc->>WalletRepo: LoadWalletWithAssetsAsync(dto.UserId)
            alt Source wallet not found
                WalletRepo-->>WalletSvc: null
                WalletSvc-->>Controller: throw ArgumentException("Error: Could not find source wallet.")
                Controller-->>Sender: 400 Bad Request
            else Source wallet loaded
                WalletSvc->>WalletRepo: GetWalletWithAssetsAsync(dto.TargetWalletAddress)
                alt Target wallet address invalid
                    WalletRepo-->>WalletSvc: null
                    WalletSvc-->>Controller: throw ArgumentException("Error: Wrong address for target wallet.")
                    Controller-->>Sender: 400 Bad Request
                else Target wallet loaded
                    WalletSvc->>WalletSvc: Check sourceWallet.Assets contains Symbol
                    alt Asset not owned by sender
                        WalletSvc-->>Controller: throw ArgumentException("Error: Could not find asset in the wallet.")
                        Controller-->>Sender: 400 Bad Request
                    else Asset owned
                        alt Insufficient asset balance (asset.Quantity < dto.AssetAmount)
                            WalletSvc-->>Controller: throw ArgumentException("Error: Transfer amount is bigger than asset quantity in the wallet.")
                            Controller-->>Sender: 400 Bad Request
                        else Transfer validated
                            alt Target wallet already owns asset
                                WalletSvc->>AssetRepo: UpdateAsync(targetAsset with incremented quantity)
                            else First time receiving asset
                                WalletSvc->>AssetRepo: AddAsync(new Asset for target wallet)
                            end

                            WalletSvc->>WalletRepo: UpdateAsync(targetWallet with updated value)

                            WalletSvc->>AssetRepo: UpdateAsync(sourceAsset with decremented quantity)
                            opt Source asset quantity drops to zero
                                WalletSvc->>AssetRepo: DeleteAsync(sourceAsset.Id)
                            end

                            WalletSvc->>WalletRepo: UpdateAsync(sourceWallet with updated value)

                            WalletSvc->>TxSvc: CreateTransactionRecordAsync(sourceWallet.Id, ..., Transfer)
                            WalletSvc->>TxSvc: CreateTransactionRecordAsync(targetWallet.Id, ..., Transfer)

                            WalletSvc->>Bus: Publish(AssetTransferEvent)
                            WalletSvc->>Bus: Publish(AssetWithdrawn)
                            WalletSvc->>Bus: Publish(AssetDeposited)

                            WalletSvc-->>Controller: Transfer complete
                            Controller-->>Gateway: 200 OK ("Transfer is successfull")
                            Gateway-->>Sender: 200 OK

                            Note over Bus,NConsumer: Queue: notifications-asset-transfer-queue
                            Bus->>NConsumer: Consume(AssetTransferEvent)
                            NConsumer->>NotifSvc: CreateNotificationAsync(SourceWalletUserId, ...)
                            NConsumer->>NotifSvc: CreateNotificationAsync(TargetWalletUserId, ...)
                            NotifSvc->>NHub: Clients.User(targetUserId).SendAsync("ReceiveNotification", ...)
                            NHub-->>Recipient: Real-time notification of incoming funds
                        end
                    end
                end
            end
        end
    end
```

---

## Process Description

1. A user transfers cryptocurrency to another internal platform user by submitting `POST /api/wallet/assets/{symbol}/transfers`. The request requires a valid JWT, an `Idempotency-Key` HTTP header, and a body containing `TargetWalletAddress` and `AssetAmount`.
2. `IdempotencyMiddleware` intercepts the request before reaching the controller. If the `Idempotency-Key` header is missing, it returns `400 Bad Request`. If the key exists in Redis (`Idemp_{key}`), it returns `409 Conflict`. Otherwise, it caches the key for 24 hours and forwards the pipeline.
3. `WalletController.TransferAsset` extracts the caller's `Guid` identity from JWT claims and invokes `WalletService.TransferAsset`.
4. `WalletService` verifies that the sender's wallet exists via `LoadWalletWithAssetsAsync`. It then looks up the recipient wallet using the destination address `0x...` via `WalletRepository.GetWalletWithAssetsAsync`.
5. The service checks asset ownership: the sender must hold an `Asset` entity matching the target symbol, and `asset.Quantity` must be greater than or equal to `dto.AssetAmount`.
6. Target wallet balances are credited: if the recipient already owns the asset, its quantity is increased; otherwise, a new `Asset` record is created for the recipient's wallet ID. Target wallet value is adjusted.
7. Sender wallet balances are debited: the sender's asset quantity is reduced. If the remaining quantity reaches zero, the `Asset` record is removed from `PortfolioDb`.
8. Two transaction records with `TransactionType.Transfer` are created via `TransactionService.CreateTransactionRecordAsync` — one audit log for the sender and one for the recipient.
9. Three messaging contracts are published to RabbitMQ: `AssetTransferEvent` for internal notifications, plus `AssetWithdrawn` and `AssetDeposited` for external account ledgers.
10. `Notifications.API` consumes `AssetTransferEvent`, creates in-app notification records in `NotificationsDb` for both participants, and pushes an immediate WebSocket alert to the recipient via `NotificationHub`.

---

## Failure Paths

- **Missing Idempotency Key:** Submitting a transfer request without the `Idempotency-Key` header causes `IdempotencyMiddleware` to abort the pipeline with `400 Bad Request`.
- **Replayed Transfer Request:** If a network retry or duplicate client action submits a previously accepted `Idempotency-Key`, Redis returns a hit and the middleware immediately responds with `409 Conflict`, guaranteeing that funds are transferred exactly once.
- **Invalid Destination Address:** If the recipient wallet address does not match any registered wallet in `PortfolioDb`, `WalletService` throws `ArgumentException("Error: Wrong address for target wallet.")`.
- **Unowned Symbol:** If the sender attempts to transfer a symbol they do not possess, `WalletService` throws `ArgumentException("Error: Could not find asset in the wallet.")`.
- **Insufficient Asset Balance:** If the requested transfer quantity exceeds the sender's current balance (`asset.Quantity < dto.AssetAmount`), the operation throws `ArgumentException("Error: Transfer amount is bigger than asset quantity in the wallet.")`.

---

## Sources

- [IdempotencyMiddleware.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Middlewares/IdempotencyMiddleware.cs#L6-L50) — Middleware validating and storing the `Idempotency-Key` header in Redis.
- [WalletController.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Controllers/WalletController.cs#L71-L91) — HTTP endpoint handling asset transfer requests.
- [WalletService.cs (TransferAsset)](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Services/WalletService.cs#L255-L331) — Core domain logic for asset verification, wallet debits/credits, and event dispatch.
- [AssetTransferEvent.cs](file:///c:/CryptoMarketProject/backend/Shared.Messages/AssetTransferEvent.cs#L5-L13) — Message contract notifying services of the transfer.
- [AssetTransferConsumer.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Consumers/AssetTransferConsumer.cs#L7-L28) — Notifications consumer recording alerts for sender and recipient.
- [TransactionService.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Services/TransactionService.cs#L21-L36) — Records double-entry transfer transaction records in `PortfolioDb`.
