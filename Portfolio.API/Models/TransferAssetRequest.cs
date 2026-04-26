namespace Portfolio.API.Models;

public class TransferAssetRequest
{
    public string TargetWalletAddress { get; set; } = string.Empty;
    public decimal AssetAmount { get; set; }
}
