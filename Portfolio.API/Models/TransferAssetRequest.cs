namespace Portfolio.API.Models;

public class TransferAssetRequest
{
    public Guid FromWalletId { get; set; }
    public string TargetWalletAddress { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public double AssetAmount { get; set; }
}
