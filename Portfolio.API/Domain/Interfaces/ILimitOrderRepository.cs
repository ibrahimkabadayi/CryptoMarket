using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;

namespace Portfolio.API.Domain.Interfaces;

public interface ILimitOrderRepository : IRepository<LimitOrder>
{
    public Task UpdateAsync(Guid id, LimitOrderStatus newStatus);
}
