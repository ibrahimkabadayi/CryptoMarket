using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Interfaces;

public interface ILimitOrderService
{
    Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto);
    Task UpdateLimitOrderAsync(Guid limitOrderId, decimal? amount, decimal? targetPrice);
    Task DeleteLimitOrderAsync(Guid limitOrderId);
    Task ApplyLimitOrder(ApplyLimitOrderDto limitOrder, decimal price);
    Task<LimitOrderDto> GetLimitOrder(Guid id);
    Task<List<LimitOrderDto>> GetAllLimitOrders();
    Task<List<LimitOrder>> GetLimitOrdersBySymbol(string symbol);
}
