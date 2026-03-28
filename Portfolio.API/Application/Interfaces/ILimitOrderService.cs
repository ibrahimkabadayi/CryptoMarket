using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Interfaces;

public interface ILimitOrderService
{
    Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto);
    Task<string> UpdateLimitOrderAsync(Guid limitOrderId, decimal? Amount, decimal? TargetPrice);
    Task DeleteLimitOrderAsync(Guid limitOrderId);
    Task<string> ApplyLimitOrder(ApplyLimitOrderDto limitOrder, decimal price);
    Task<LimitOrderDto> GetLimitOrder(Guid id);
    Task<List<LimitOrderDto>> GetAllLimitOrders();
    Task<List<LimitOrder>> GetLimitOrdersBySymbol(string symbol);
}
