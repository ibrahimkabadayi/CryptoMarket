using AutoMapper;
using MassTransit;
using Notifications.API.Application.Interfaces;
using Notifications.API.Application.Services;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Enums;
using Shared.Messages;

namespace Notifications.API.Consumers;

public class CoinPriceConsumer(
    IPriceAlertService priceAlertService,
    INotificationService notificationService,
    ICacheService cacheService) : IConsumer<CoinPriceEvent>
{
    public async Task Consume(ConsumeContext<CoinPriceEvent> context)
    {
        var message = context.Message;

        var key = message.Symbol + "Alerts";

        var activeAlerts = await cacheService.GetAsync<List<PriceAlert>>(key);
        activeAlerts ??= await priceAlertService.GetActiveAlertsBySymbolAsync(message.Symbol);

        bool isCacheChanged = false;

        if (activeAlerts == null)
        {
            activeAlerts = await priceAlertService.GetActiveAlertsBySymbolAsync(message.Symbol);

            isCacheChanged = true;
        }

        foreach (var alert in activeAlerts)
        {
            if (alert == null || !alert.IsActive) continue;

            bool isTriggered = false;

            if (alert.IsAbove && message.Price >= alert.TargetPrice)
            {
                isTriggered = true;
                Console.WriteLine($"[ALARM] {alert.Symbol} fiyatı hedefin ({alert.TargetPrice}) üstüne çıktı! Anlık: {message.Price}");
            }
            else if (!alert.IsAbove && message.Price <= alert.TargetPrice)
            {
                isTriggered = true;
                Console.WriteLine($"[ALARM] {alert.Symbol} fiyatı hedefin ({alert.TargetPrice}) altına düştü! Anlık: {message.Price}");
            }

            if (isTriggered)
            {
                try
                {
                    string direction = alert.IsAbove ? "üzerine çıktı" : "altına düştü";
                    string notificationMsg = $"{alert.Symbol} fiyatı belirlediğiniz {alert.TargetPrice} hedefinin {direction}. Anlık Fiyat: {message.Price}";

                    await notificationService.CreateNotificationAsync(
                        userId: alert.UserId,
                        title: $"🎯 {alert.Symbol} Fiyat Alarmı!",
                        message: notificationMsg,
                        type: NotificationType.PriceAlert,
                        relatedEntityId: alert.Id.ToString()
                    );

                    await priceAlertService.DeactivateAlertAsync(alert.Id, alert.UserId);

                    alert.Deactivate();
                    isCacheChanged = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HATA BİLDİRİM OLUŞTURULAMADI]: {ex.Message}");
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