using AutoMapper;
using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class CoinPriceConsumer(ILimitOrderService limitOrderService, ICacheService cacheService, IMapper mapper) : IConsumer<CoinPriceEvent>
{
    public async Task Consume(ConsumeContext<CoinPriceEvent> context)
    {
        var message = context.Message;

        var key = message.Symbol + "Orders";

        var limitOrders = await cacheService.GetAsync<List<LimitOrder>>(key);
        limitOrders ??= await limitOrderService.GetLimitOrdersBySymbol(message.Symbol);

        foreach (var order in limitOrders)
        {
            if(order == null) continue;

            if (order.OrderType == LimitOrderType.Buy && order.TargetPrice <= message.Price && order.OrderStatus == LimitOrderStatus.Pending) 
            {
                var dto = mapper.Map<ApplyLimitOrderDto>(order);
                await limitOrderService.ApplyLimitOrder(dto, message.Price);
                Console.WriteLine($"Found one: Applying buy order at {message.Price}");

                order.OrderStatus = LimitOrderStatus.Filled;
                await cacheService.SetAsync(key, limitOrders);
            }
            else if (order.OrderType == LimitOrderType.Sell && order.TargetPrice >= message.Price && order.OrderStatus == LimitOrderStatus.Pending)
            {
                var dto = mapper.Map<ApplyLimitOrderDto>(order);
                await limitOrderService.ApplyLimitOrder(dto, message.Price);
                Console.WriteLine($"Found one: Applying sell order at {message.Price}");

                order.OrderStatus = LimitOrderStatus.Filled;
                await cacheService.SetAsync(key, limitOrders);
            }
        }
    }
}
