namespace Shared.Messages;

public record AssetWithdrawn(
    Guid WalletId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt
);
