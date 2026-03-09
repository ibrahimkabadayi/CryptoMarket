using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;

namespace Market.API.Infrastructure.Repositories;

public class PriceAlertRepository(MarketDbContext context) : Repository<PriceAlert>(context, "PriceAlerts"), IPriceAlertRepository
{
}
