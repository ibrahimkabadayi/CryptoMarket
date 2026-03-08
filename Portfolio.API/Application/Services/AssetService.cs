using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;

namespace Portfolio.API.Application.Services;

public class AssetService(IAssetRepository assetRepository) : IAssetService
{
    public async Task<Asset> AddAssetToWalletThatHasThatAsset(Asset asset, double buyingPrice, double quantity)
    {
        var currentAssetValue = asset.Quantity * asset.AverageBuyPrice;
        currentAssetValue += buyingPrice * quantity;

        var newAveragePrice = currentAssetValue / quantity;
        asset.AverageBuyPrice = newAveragePrice;
        asset.Quantity += quantity;
        asset.UpdatedDate = DateTime.UtcNow;

        await assetRepository.UpdateAsync(asset);
        return asset;
    }
}
