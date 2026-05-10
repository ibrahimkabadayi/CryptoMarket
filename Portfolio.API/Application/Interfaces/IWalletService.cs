using Portfolio.API.Application.DTOs;

namespace Portfolio.API.Application.Interfaces;

public interface IWalletService
{
    Task CreateWallet(Guid userId);
    Task DepositMoney(Guid walletId, decimal amount);
    Task WithdrawMoney(Guid walletId, decimal amount);
    Task TransferAsset(TransferAssetDto dto);
    Task<bool> BuyAsset(Guid walletId, string symbol, decimal currentPrice, decimal amount, bool isLimitOrder);
    Task SellAsset(Guid walletId, string symbol, decimal price, decimal amount, bool isLimitOrder);
    Task<Guid> GetWalletIdByUserId(Guid userId);
    Task<PortfolioDashboardDto> GetPortfolioDashboardAsync(Guid userId);
}
