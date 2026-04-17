namespace Portfolio.API.Application.Interfaces;

public interface ITreasureBalanceService
{
    Task AddAssetViaFee(string assetSymbol, decimal amount);
    Task AddAssetViaFee(decimal amount);
}
