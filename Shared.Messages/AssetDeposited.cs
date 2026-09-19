namespace Shared.Messages;

public record AssetDeposited(
    Guid WalletId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt
);
