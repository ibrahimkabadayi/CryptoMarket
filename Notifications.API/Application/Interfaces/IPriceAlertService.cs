using Notifications.API.Application.DTOs;
using Notifications.API.Domain.Entities;

namespace Notifications.API.Application.Interfaces;

public interface IPriceAlertService
{
    Task CreateAlertAsync(Guid userId, string symbol, decimal targetPrice, bool isAbove);
    Task<IEnumerable<PriceAlertDto>> GetActiveAlertsByUserAsync(Guid userId);
    Task<IEnumerable<PriceAlertDto>> GetAllAlertsByUserAsync(Guid userId);
    Task DeactivateAlertAsync(Guid alertId, Guid userId);
    Task<List<PriceAlert>> GetActiveAlertsBySymbolAsync(string symbol);
}
