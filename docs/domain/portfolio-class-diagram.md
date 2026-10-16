# Portfolio Service Domain Model

> Domain class diagram for `Portfolio.API`, representing the `Wallet` aggregate root, entity relationships, invariant-enforcing domain services, and repository abstractions.

---

## Class Diagram

```mermaid
classDiagram
    %% Last verified against commit: b5229aa

    class BaseEntity {
        <<Entity>>
        +Guid Id
        +DateTime CreatedDate
        +DateTime UpdatedDate
    }

    class Wallet {
        <<AggregateRoot>>
        +Guid UserId
        +string Address
        +decimal FiatBalance
        +decimal Value
        +uint RowVersion
        +ICollection~Asset~ Assets
        +ICollection~LimitOrder~ LimitOrders
    }

    class Asset {
        <<Entity>>
        +Guid WalletId
        +string Symbol
        +decimal Quantity
        +decimal CostBasis
    }

    class LimitOrder {
        <<Entity>>
        +Guid WalletId
        +Guid UserId
        +string Symbol
        +decimal TargetPrice
        +decimal Amount
        +LimitOrderType OrderType
        +LimitOrderStatus OrderStatus
    }

    class Transaction {
        <<Entity>>
        +Guid WalletId
        +string Symbol
        +decimal Amount
        +decimal Price
        +TransactionType TransactionType
        +DateTime TransactionDate
    }

    class LimitOrderType {
        <<enumeration>>
        Buy
        Sell
    }

    class LimitOrderStatus {
        <<enumeration>>
        Pending
        Processing
        Filled
        Canceled
    }

    class TransactionType {
        <<enumeration>>
        Deposit
        Withdraw
        Buy
        Sell
        Transfer
    }

    class IWalletService {
        <<Interface>>
        +BuyAsset(userId, symbol, currentPrice, amount, isLimitOrder)
        +BuyAssetWithUserId(userId, symbol, currentPrice, amount, isLimitOrder)
        +SellAsset(userId, symbol, price, amount, isLimitOrder)
        +TransferAsset(dto)
        +DepositMoney(userId, amount)
        +WithdrawMoney(userId, amount)
        +GetPortfolioDashboardAsync(userId) PortfolioDashboardDto
        +CreateWallet(userId)
    }

    class ILimitOrderService {
        <<Interface>>
        +CreateLimitOrderAsync(dto, correlationId)
        +CheckLimitOrders(symbol, price)
        +ApplyLimitOrder(dto, price)
        +DeleteLimitOrderAsync(id)
        +GetLimitOrdersBySymbol(symbol) List~LimitOrder~
    }

    class ITransactionService {
        <<Interface>>
        +CreateTransactionRecordAsync(walletId, amount, type)
        +CreateTransactionRecordAsync(walletId, symbol, amount, price, type)
        +GetTenLastTransaction(walletId) List~TransactionDto~
    }

    BaseEntity <|-- Wallet
    BaseEntity <|-- Asset
    BaseEntity <|-- LimitOrder
    BaseEntity <|-- Transaction

    Wallet "1" *-- "0..*" Asset : owns
    Wallet "1" *-- "0..*" LimitOrder : holds
    Wallet "1" --> "0..*" Transaction : logs

    LimitOrder --> LimitOrderType : classified by
    LimitOrder --> LimitOrderStatus : tracked by
    Transaction --> TransactionType : categorized by

    IWalletService ..> Wallet : mutates balance & assets
    ILimitOrderService ..> LimitOrder : matches & updates state
    ITransactionService ..> Transaction : records audit trail
```

---

## Domain Architecture and Invariants

### 1. The Wallet Aggregate Root
`Wallet` serves as the primary aggregate root within the Portfolio domain:
- **One Wallet per User:** Enforced by database unique constraint `IX_Wallets_UserId` and verified upon creation.
- **Child Asset Encapsulation:** Individual crypto balances (`Asset` entities) are owned by the `Wallet`. When an asset quantity drops to zero through sales or transfers, the `Asset` row is pruned by `WalletService`.
- **Concurrency Control:** The `RowVersion` uint column acts as an optimistic concurrency token to safeguard wallet balance modifications against race conditions during simultaneous deposit, withdrawal, and trade operations.

### 2. Domain Services and Invariant Enforcement
- **`WalletService`:** Enforces core financial invariants:
  - Total cost validation: `wallet.FiatBalance >= totalCost` for purchases.
  - Ownership validation: caller must own sufficient quantity of `Asset` before initiating sales or transfers.
  - Fee calculation: automatically applies `MakerFeeRate` for limit order executions and `TakerFeeRate` for immediate market purchases.
  - Double-entry transfer: debits sender, credits receiver, adjusts wallet valuations, and logs dual transaction records.
- **`LimitOrderService`:** Coordinates the price matching engine:
  - Enforces order uniqueness via Redis CorrelationId checks.
  - Manages concurrent evaluation of order execution via `Parallel.ForEachAsync`.
  - Publishes `LimitOrderOccuredEvent` upon filled order transitions.
- **`TransactionService`:** Maintains an immutable audit history of all money and crypto movements (`Deposit`, `Withdraw`, `Buy`, `Sell`, `Transfer`).

---

## Sources

- [Wallet.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Domain/Entities/Wallet.cs#L5-L16) — Aggregate root entity.
- [Asset.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Domain/Entities/Asset.cs#L5-L13) — Owned asset entity.
- [LimitOrder.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Domain/Entities/LimitOrder.cs#L5-L15) — Limit order entity.
- [Transaction.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Domain/Entities/Transaction.cs#L5-L15) — Financial transaction audit entity.
- [IWalletService.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Interfaces/IWalletService.cs#L8-L23) — Wallet service contract.
- [ILimitOrderService.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Interfaces/ILimitOrderService.cs#L7-L18) — Limit order service contract.
- [ITransactionService.cs](file:///c:/CryptoMarketProject/backend/Portfolio.API/Application/Interfaces/ITransactionService.cs#L6-L15) — Transaction service contract.
