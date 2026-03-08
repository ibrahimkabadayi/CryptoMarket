namespace Shared.Messages;

public record AssetTransferEvent
{
    public string Message { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public Guid SourceWalletUserId { get; set; }
    public Guid TargetWalletUserId { get; set; }
}
