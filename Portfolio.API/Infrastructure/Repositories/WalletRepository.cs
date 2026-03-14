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

    public async Task<Wallet> GetWalletWithAssetsAsync(Guid WalletId)
    {
        return await context.Wallets
            .Include(x => x.Assets)
            .Where(x => x.Id == WalletId)
            .FirstAsync();
    }

    public async Task<Wallet> GetWalletWithAssetsAsync(string WalletAddress)
    {
        return await context.Wallets
            .Include(x => x.Assets)
            .Where(x => x.Address == WalletAddress)
            .FirstAsync();
    }
}
