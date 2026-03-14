using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Domain.Interfaces;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Wallet> GetWalletWithAssetsAsync(Guid WalletId);
    Task<Wallet> GetWalletWithAssetsAsync(string WalletAddress);
    Task<Guid> GetWalletIdByUserId(Guid userId);
}
