using MassTransit;
using Notifications.API.Application.Interfaces;
using Shared.Messages;

namespace Notifications.API.Consumers;

public class CoinPriceConsumer(IPriceAlertService priceAlertService) : IConsumer<CoinPriceEvent>
{
    public async Task Consume(ConsumeContext<CoinPriceEvent> context)
    {
        var message = context.Message;
        await priceAlertService.CheckPriceAlerts(message.Symbol, message.Price);
    }
}