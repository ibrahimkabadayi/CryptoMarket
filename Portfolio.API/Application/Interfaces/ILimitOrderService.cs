using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Interfaces;

public interface ILimitOrderService
{
    Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto, Guid correlationId);
    Task UpdateLimitOrderAsync(Guid limitOrderId, decimal? amount, decimal? targetPrice);
    Task DeleteLimitOrderAsync(Guid limitOrderId);
    Task ApplyLimitOrder(ApplyLimitOrderDto limitOrder, decimal price);
    Task CheckLimitOrders(string symbol, decimal price);
    Task<LimitOrderDto> GetLimitOrder(Guid id);
    Task<List<LimitOrderDto>> GetAllLimitOrders();
    Task<List<LimitOrder>> GetLimitOrdersBySymbol(string symbol);
}
