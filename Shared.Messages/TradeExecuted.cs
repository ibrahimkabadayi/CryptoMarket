namespace Shared.Messages;

public record TradeExecuted(
    Guid WalletId,
    decimal BoughtAmount,
    string BoughtCurrency,
    decimal SoldAmount,
    string SoldCurrency,
    DateTime OccurredAt
);
