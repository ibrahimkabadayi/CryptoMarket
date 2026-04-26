using Microsoft.EntityFrameworkCore;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class WalletRepository(ApplicationDbContext context) : Repository<Wallet>(context), IWalletRepository
{
    public async Task<Guid> GetWalletIdByUserId(Guid userId)
    {
        var wallet =  await context.Wallets.FirstAsync(x => x.UserId == userId);
        return wallet.Id;
    }

    public async Task<Wallet> GetWalletWithAssetsAsync(Guid walletId)
    {
        return await context.Wallets
            .Include(x => x.Assets)
            .Where(x => x.Id == walletId)
            .FirstAsync();
    }

    public async Task<Wallet> GetWalletWithAssetsAsync(string walletAddress)
    {
        return await context.Wallets
            .Include(x => x.Assets)
            .Where(x => x.Address == walletAddress)
            .FirstAsync();
    }
}
