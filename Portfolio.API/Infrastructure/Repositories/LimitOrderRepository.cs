using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Infrastructure.Repositories;

public class LimitOrderRepository(ApplicationDbContext context) : Repository<LimitOrder>(context), ILimitOrderRepository
{
    public async Task UpdateAsync(Guid Id, LimitOrderStatus newStatus)
    {
        var entity = await GetByIdAsync(Id);
        if (entity == null) return;

        if (newStatus == LimitOrderStatus.Filled)
            entity.Fill();
        else
            entity.UpdateStatus(newStatus);
        
        context.LimitOrders.Update(entity);
        await context.SaveChangesAsync();
    }
}
