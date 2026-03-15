using Portfolio.API.Application.DTOs;

namespace Portfolio.API.Application.Interfaces;

public interface IWalletService
{
    Task<string> CreateWallet(Guid UserId);
    Task<string> DepositMoney(Guid WalletId, decimal Amount);
    Task<string> DepositMoney(string WalletAddress, decimal Amount);
    Task WithdrawMoney(Guid WalletId, decimal Amount);
    Task<string> TransferAsset(TransferAssetDto dto);
    Task<string> BuyAsset(Guid WalletId, string Symbol, decimal CurrentPrice, decimal Amount);
    Task<string> SellAsset(Guid WalletId, string Symbol, decimal Amount);
    Task<Guid> GetWalletIdByUserId(Guid userId);
}
