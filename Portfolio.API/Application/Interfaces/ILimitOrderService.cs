using Portfolio.API.Application.DTOs;

namespace Portfolio.API.Application.Interfaces;

public interface ILimitOrderService
{
    Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto);
    Task<string> UpdateLimitOrderAsync(Guid limitOrderId, decimal? Amount, decimal? TargetPrice);
    Task DeleteLimitOrderAsync(Guid limitOrderId);
    Task<string> ApplyLimitOrder(LimitOrderDto limitOrder);
    Task<LimitOrderDto> GetLimitOrder(Guid id);
    Task<List<LimitOrderDto>> GetAllLimitOrders();
}
