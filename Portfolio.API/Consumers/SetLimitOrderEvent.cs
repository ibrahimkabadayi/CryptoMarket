using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Enums;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class SetLimitOrderEvent(ILimitOrderService limitOrderService, IWalletService walletService, ICacheService cacheService) : IConsumer<LimitOrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<LimitOrderPlacedEvent> context)
    {
        var message = context.Message;

        var result = await cacheService.GetAsync<string>(message.CorrelationId.ToString());

        if (result != null)
        {
            throw new InvalidOperationException("This order already done");
        }

        await cacheService.SetAsync<string>(message.CorrelationId.ToString(), message.CorrelationId.ToString(), TimeSpan.FromHours(24));

        var walletId = await walletService.GetWalletIdByUserId(message.UserId);

        LimitOrderType orderType;

        if (message.OrderType.Equals("Buy"))
        {
            orderType = LimitOrderType.Buy;
        } 
        else if (message.OrderType.Equals("Sell"))
        {
            orderType = LimitOrderType.Sell;
        }
        else
        {
            throw new ArgumentException("Order Type must be either buy or sell.");
        }

        var createOrder = new CreateLimitOrderDto
        {
            Amount = message.Amount,
            WalletId = walletId,
            UserId = message.UserId,
            TargetPrice = message.TargetPrice,
            OrderType = orderType
        };

        await limitOrderService.CreateLimitOrderAsync(createOrder);
    }
}
