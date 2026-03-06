using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class WalletRepository(ApplicationDbContext context) : Repository<Wallet>(context), IWalletRepository
{
}
