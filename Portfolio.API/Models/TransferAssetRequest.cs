namespace Portfolio.API.Models;

public class TransferAssetRequest
{
    public Guid FromWalletId { get; set; }
    public string TargetWalletAddress { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double AssetAmount { get; set; }
}
