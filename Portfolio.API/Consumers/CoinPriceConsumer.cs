using MassTransit;
using Portfolio.API.Application.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class CoinPriceConsumer(ILimitOrderService limitOrderService) : IConsumer<CoinPriceEvent>
{
    public async Task Consume(ConsumeContext<CoinPriceEvent> context)
    {
        var message = context.Message;
        await limitOrderService.CheckLimitOrders(message.Symbol, message.Price);
    }
}