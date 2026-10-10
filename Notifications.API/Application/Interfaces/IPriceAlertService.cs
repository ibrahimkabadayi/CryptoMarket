using Notifications.API.Application.DTOs;
using Notifications.API.Domain.Entities;

namespace Notifications.API.Application.Interfaces;

public interface IPriceAlertService
{
    Task<PriceAlertDto> CreateAlertAsync(string userId, string symbol, decimal targetPrice, bool isAbove);
    Task<IEnumerable<PriceAlertDto>> GetActiveAlertsByUserAsync(string userId);
    Task<IEnumerable<PriceAlertDto>> GetAllAlertsByUserAsync(string userId);
    Task DeactivateAlertAsync(Guid alertId, string userId);
    Task<List<PriceAlert>> GetActiveAlertsBySymbolAsync(string symbol);
    Task CheckPriceAlerts(string symbol, decimal price);
}
