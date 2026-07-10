using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;

namespace Market.API.Infrastructure.Repositories;

public class MarketNewsRepository(ApplicationDbContext context) : Repository<MarketNews>(context), IMarketNewsRepository
{
}
