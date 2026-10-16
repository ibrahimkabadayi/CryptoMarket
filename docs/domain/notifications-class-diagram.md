# Notifications Service Domain Model

> Domain class diagram for `Notifications.API`, representing notification dispatches, price alerts, SignalR real-time hubs, and domain services.

---

## Class Diagram

```mermaid
classDiagram
    %% Last verified against commit: be4cbee

    class BaseEntity {
        <<Entity>>
        +Guid Id
        +DateTime CreatedDate
        +DateTime UpdatedDate
    }

    class Notification {
        <<AggregateRoot>>
        +Guid UserId
        +string Title
        +string Message
        +NotificationType Type
        +bool IsRead
        +DateTime? ReadAt
        +string RelatedEntityId
        +MarkAsRead()
    }

    class PriceAlert {
        <<AggregateRoot>>
        +Guid UserId
        +string Symbol
        +decimal TargetPrice
        +bool IsAbove
        +bool IsActive
        +DateTime CreatedAt
        +Deactivate()
    }

    class NotificationType {
        <<enumeration>>
        System
        PriceAlert
        OrderFilled
        Transfer
        Welcome
    }

    class INotificationService {
        <<Interface>>
        +CreateNotificationAsync(userId, title, message, type, relatedEntityId)
        +GetUserNotificationsAsync(userId) List~NotificationDto~
        +GetUnreadNotificationCountAsync(userId) int
        +MarkAsReadAsync(notificationId, userId)
        +MarkAllAsReadAsync(userId)
    }

    class IPriceAlertService {
        <<Interface>>
        +AddPriceAlertAsync(userId, symbol, targetPrice, isAbove)
        +GetActiveAlertsBySymbolAsync(symbol) List~PriceAlert~
        +CheckPriceAlertsAsync(symbol, currentPrice)
        +DeactivatePriceAlertAsync(alertId, userId)
    }

    class INotificationHubService {
        <<Interface>>
        +SendNotificationToUserAsync(userId, notification)
        +SendPriceAlertToUserAsync(userId, alert)
    }

    BaseEntity <|-- Notification
    BaseEntity <|-- PriceAlert

    Notification --> NotificationType : categorized by

    INotificationService ..> Notification : manages & mutates
    IPriceAlertService ..> PriceAlert : checks & deactivates
    INotificationService ..> INotificationHubService : broadcasts real-time
```

---

## Domain Architecture and Invariants

### 1. Notification Aggregate Root
- **Lifecycle:** Created with `IsRead = false`. Transition to read status occurs explicitly via `Notification.MarkAsRead()`, which stamps `ReadAt = DateTime.UtcNow`.
- **Classification:** Categorized by `NotificationType` enum (`System`, `PriceAlert`, `OrderFilled`, `Transfer`, `Welcome`).
- **Traceability:** Carries an optional `RelatedEntityId` to link back to the originating trade, order, or transfer.

### 2. PriceAlert Aggregate Root
- **Threshold Matching:** Configured with `TargetPrice` and directional boolean `IsAbove`. When incoming `CoinPriceEvent` ticks breach the target threshold, the alert triggers a user notification.
- **Single-Trigger Invariant:** Price alerts are deactivated upon matching via `PriceAlert.Deactivate()` (`IsActive = false`), preventing duplicate alert spam for oscillating prices.

---

## Sources

- [Notification.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Domain/Entities/Notification.cs#L5-L44) — Notification domain entity.
- [PriceAlert.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Domain/Entities/PriceAlert.cs#L5-L49) — PriceAlert domain entity.
- [NotificationType.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Domain/Enums/NotificationType.cs#L3-L10) — Notification types.
- [INotificationService.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Application/Interfaces/INotificationService.cs#L6-L14) — Notification service interface.
- [IPriceAlertService.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Application/Interfaces/IPriceAlertService.cs#L6-L13) — Price alert service interface.
- [INotificationHubService.cs](file:///c:/CryptoMarketProject/backend/Notifications.API/Application/Interfaces/INotificationHubService.cs#L6-L11) — Notification SignalR hub contract.

