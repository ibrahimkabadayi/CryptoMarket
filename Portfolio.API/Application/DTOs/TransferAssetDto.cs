namespace Portfolio.API.Application.DTOs;

public class TransferAssetDto
{
    public Guid FromWalletId { get; set; }
    public string TargetWalletAddress { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal AssetAmount { get; set; }
}
