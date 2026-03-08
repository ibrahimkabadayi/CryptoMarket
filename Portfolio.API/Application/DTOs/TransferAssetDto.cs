namespace Portfolio.API.Application.DTOs;

public class TransferAssetDto
{
    public Guid FromWalletId { get; set; }
    public string TargetWalletAddress { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public double AssetAmount { get; set; }
}
