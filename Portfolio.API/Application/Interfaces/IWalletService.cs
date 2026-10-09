using Portfolio.API.Application.DTOs;

namespace Portfolio.API.Application.Interfaces;

public interface IWalletService
{
    Task CreateWallet(Guid userId);
    Task DepositMoney(string userId, decimal amount);
    Task WithdrawMoney(string userId, decimal amount);
    Task TransferAsset(TransferAssetDto dto);
    Task BuyAsset(string userId, string symbol, decimal currentPrice, decimal amount, bool isLimitOrder);
    Task BuyAssetWithUserId(string userId, string symbol, decimal currentPrice, decimal amount, bool isLimitOrder);
    Task SellAsset(string userId, string symbol, decimal price, decimal amount, bool isLimitOrder);
    Task<Guid> GetWalletIdByUserId(Guid userId);
    Task<PortfolioDashboardDto> GetPortfolioDashboardAsync(string userId);
}
