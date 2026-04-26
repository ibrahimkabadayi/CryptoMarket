using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Domain.Interfaces;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Wallet> GetWalletWithAssetsAsync(Guid walletId);
    Task<Wallet> GetWalletWithAssetsAsync(string walletAddress);
    Task<Guid> GetWalletIdByUserId(Guid userId);
}
