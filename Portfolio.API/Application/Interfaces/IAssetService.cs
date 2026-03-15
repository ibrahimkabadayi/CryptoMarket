using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Interfaces;

public interface IAssetService
{
    Task<Asset> AddAssetToWalletThatHasThatAsset(Asset asset, decimal buyingPrice, decimal quantity);
}
