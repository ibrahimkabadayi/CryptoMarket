using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Enums;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Hubs;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace Notifications.API.Application.Services;

public class PriceAlertService(
    IPriceAlertRepository priceAlertRepository,
    IMapper mapper,
    INotificationService notificationService,
    ICacheService cacheService,
    IHubContext<NotificationHub> hubContext
    ) : IPriceAlertService
{
    public async Task<PriceAlertDto> CreateAlertAsync(Guid userId, string symbol, decimal targetPrice, bool isAbove)
    {
        var alert = new PriceAlert(userId, symbol, targetPrice, isAbove);

        await priceAlertRepository.AddAsync(alert);

        var key = alert.Symbol + "Alerts";
        await cacheService.RemoveAsync(key);

        return mapper.Map<PriceAlertDto>(alert);
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
            throw new Exception("Alert not found or unauthorized access.");
        }

        alert.Deactivate();

        var key = alert.Symbol + "Alerts";
        await cacheService.RemoveAsync(key);

        await priceAlertRepository.UpdateAsync(alert);

        await hubContext.Clients.User(userId.ToString())
                    .SendAsync("DeactivatePriceAlert", alertId);
    }

    public async Task<List<PriceAlert>> GetActiveAlertsBySymbolAsync(string symbol)
    {
        var alerts = await priceAlertRepository.FindAsync(a => a.IsActive && a.Symbol == symbol);
        return [.. alerts];
    }

    public async Task CheckPriceAlerts(string symbol, decimal price)
    {
        var key = symbol + "Alerts";

        var activeAlerts = await cacheService.GetAsync<List<PriceAlert>>(key);
        activeAlerts ??= await GetActiveAlertsBySymbolAsync(symbol);

        bool isCacheChanged = false;

        if (activeAlerts == null)
        {
            activeAlerts = await GetActiveAlertsBySymbolAsync(symbol);

            isCacheChanged = true;
        }

        foreach (var alert in activeAlerts)
        {
            if (alert == null || !alert.IsActive) continue;

            bool isTriggered = false;

            if (alert.IsAbove && price >= alert.TargetPrice)
            {
                isTriggered = true;
                Console.WriteLine($"[ALARM] The price of {alert.Symbol} has rose above the target price ({alert.TargetPrice})! Now: {message.Price}");
            }
            else if (!alert.IsAbove && price <= alert.TargetPrice)
            {
                isTriggered = true;
                Console.WriteLine($"[ALARM] The price of {alert.Symbol} has fallen below the target price ({alert.TargetPrice})! Now: {message.Price}");
            }

            if (isTriggered)
            {
                try
                {
                    string direction = alert.IsAbove ? "rose above" : "fell blow";
                    string notificationMsg = $"{alert.Symbol} is the price you set for the {alert.TargetPrice} target in {direction}. Current Price: {message.Price}"

                    await notificationService.CreateNotificationAsync(
                        userId: alert.UserId,
                        title: $"{alert.Symbol} price alert!",
                        message: notificationMsg,
                        type: NotificationType.PriceAlert,
                        relatedEntityId: alert.Id.ToString()
                    );

                    await DeactivateAlertAsync(alert.Id, alert.UserId);

                    alert.Deactivate();
                    isCacheChanged = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error Notification could not be created]: {ex.Message}");
                }
            }
        }

        if (isCacheChanged)
        {
            var updatedAlerts = activeAlerts.FindAll(a => a.IsActive);
            await cacheService.SetAsync(key, updatedAlerts, TimeSpan.FromSeconds(5));
        }
    }
}
