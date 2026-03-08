using Portfolio.API.Application.DTOs;

namespace Portfolio.API.Application.Interfaces;

public interface IWalletService
{
    Task<string> CreateWallet(Guid UserId);
    Task<string> DepositMoney(Guid WalletId, double Amount);
    Task<string> DepositMoney(string WalletAddress, double Amount);
    Task<string> TransferAsset(TransferAssetDto dto);
    Task<string> BuyAsset(Guid WalletId, string Symbol, double CurrentPrice, double Amount);
}
