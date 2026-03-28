using AutoMapper;
using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Services;
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
                order.OrderStatus = LimitOrderStatus.Proccesing;

                var dtosToCache = mapper.Map<List<LimitOrderCacheDto>>(limitOrders);
                await cacheService.SetAsync(key, dtosToCache, TimeSpan.FromSeconds(5));

                Console.WriteLine($"Found one: Applying buy order at {message.Price}");

                var dto = mapper.Map<ApplyLimitOrderDto>(order);
                var result = await limitOrderService.ApplyLimitOrder(dto, message.Price);

                if (!result.StartsWith("Success"))
                {
                    Console.WriteLine($"[HATA BAŞARISIZ EMİR]: {result}");
                    order.OrderStatus = LimitOrderStatus.Pending;
                    dtosToCache = mapper.Map<List<LimitOrderCacheDto>>(limitOrders);
                    await cacheService.SetAsync(key, dtosToCache, TimeSpan.FromSeconds(5));
                }
            }
            else if (order.OrderType == LimitOrderType.Sell && order.TargetPrice >= message.Price && order.OrderStatus == LimitOrderStatus.Pending)
            {
                order.OrderStatus = LimitOrderStatus.Proccesing;

                var dtosToCache = mapper.Map<List<LimitOrderCacheDto>>(limitOrders);
                await cacheService.SetAsync(key, dtosToCache, TimeSpan.FromSeconds(5));

                Console.WriteLine($"Found one: Applying sell order at {message.Price}");

                var dto = mapper.Map<ApplyLimitOrderDto>(order);
                var result = await limitOrderService.ApplyLimitOrder(dto, message.Price);

                if (!result.StartsWith("Success"))
                {
                    Console.WriteLine($"[HATA BAŞARISIZ EMİR]: {result}");
                    order.OrderStatus = LimitOrderStatus.Pending;
                    dtosToCache = mapper.Map<List<LimitOrderCacheDto>>(limitOrders);
                    await cacheService.SetAsync(key, dtosToCache, TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}
