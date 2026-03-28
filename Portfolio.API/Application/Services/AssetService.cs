using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;

namespace Portfolio.API.Application.Services;

public class AssetService(IAssetRepository assetRepository) : IAssetService
{
    public async Task<Asset> AddAssetToWalletThatHasThatAsset(Asset asset, decimal buyingPrice, decimal quantity)
    {
        var currentAssetValue = (asset.Quantity * asset.AverageBuyPrice) + (buyingPrice * quantity);

        var totalQuantity = asset.Quantity + quantity;
        var newAveragePrice = currentAssetValue / totalQuantity;

        asset.AverageBuyPrice = newAveragePrice;
        asset.Quantity = totalQuantity;
        asset.UpdatedDate = DateTime.UtcNow;

        await assetRepository.UpdateAsync(asset);
        return asset;
    }
}
