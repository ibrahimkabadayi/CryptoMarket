using AutoMapper;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Infrastructure.Repositories;

namespace Notifications.API.Application.Services;

public class PriceAlertService(IPriceAlertRepository priceAlertRepository, IMapper mapper) : IPriceAlertService
{
    public async Task CreateAlertAsync(Guid userId, string symbol, decimal targetPrice, bool isAbove)
    {
        var alert = new PriceAlert(userId, symbol, targetPrice, isAbove);

        await priceAlertRepository.AddAsync(alert);
    }

    public async Task<IEnumerable<PriceAlertDto>> GetActiveAlertsByUserAsync(Guid userId)
    {
        var alerts = await priceAlertRepository.FindAsync(a => a.UserId == userId && a.IsActive);
        var orderedAlerts = alerts.OrderByDescending(a => a.CreatedAt).ToList();

        return mapper.Map<IEnumerable<PriceAlertDto>>(orderedAlerts);
    }

    public async Task<IEnumerable<PriceAlertDto>> GetAllAlertsByUserAsync(Guid userId)
    {
        var alerts = await priceAlertRepository.FindAsync(a => a.UserId == userId);
        var orderedAlerts = alerts.OrderByDescending(a => a.CreatedAt).ToList();

        return mapper.Map<IEnumerable<PriceAlertDto>>(orderedAlerts);
    }

    public async Task DeactivateAlertAsync(Guid alertId, Guid userId)
    {
        var alert = await priceAlertRepository.GetByIdAsync(alertId);

        if (alert == null || alert.UserId != userId)
        {
            throw new Exception("Alarm bulunamadı veya yetkisiz erişim işlemi.");
        }

        alert.Deactivate();

        await priceAlertRepository.UpdateAsync(alert);
    }

    public async Task<List<PriceAlert>> GetActiveAlertsBySymbolAsync(string symbol)
    {
        var alerts = await priceAlertRepository.FindAsync(a => a.IsActive && a.Symbol == symbol);
        return alerts.ToList();
    }
}
