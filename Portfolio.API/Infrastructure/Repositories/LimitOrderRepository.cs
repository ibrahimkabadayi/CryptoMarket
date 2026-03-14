using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class LimitOrderRepository(ApplicationDbContext context) : Repository<LimitOrder>(context), ILimitOrderRepository
{
}
