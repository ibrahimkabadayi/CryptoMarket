using Portfolio.API.Application.DTOs;

namespace Portfolio.API.Application.Interfaces;

public interface IWalletService
{
    Task<string> CreateWallet(Guid userId);
    Task<string> DepositMoney(Guid walletId, decimal amount);
    Task WithdrawMoney(Guid walletId, decimal amount);
    Task<string> TransferAsset(TransferAssetDto dto);
    Task<string> BuyAsset(Guid walletId, string symbol, decimal currentPrice, decimal amount, bool isLimitOrder);
    Task<string> SellAsset(Guid walletId, string symbol, decimal price, decimal amount, bool isLimitOrder);
    Task<Guid> GetWalletIdByUserId(Guid userId);
    Task<PortfolioDashboardDto> GetPortfolioDashboardAsync(Guid userId);
}
