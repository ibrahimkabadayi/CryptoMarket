using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Infrastructure.Context;

namespace Notifications.API.Infrastructure.Repositories;

public class PriceAlertRepository(ApplicationDbContext context) : Repository<PriceAlert>(context), IPriceAlertRepository
{
}
