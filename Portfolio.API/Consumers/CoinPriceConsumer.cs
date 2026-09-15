using AutoMapper;
using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class CoinPriceConsumer(
    ILimitOrderService limitOrderService,
    ICacheService cacheService,
    IMapper mapper,
    ILogger<CoinPriceConsumer> logger)
    : IConsumer<CoinPriceEvent>
{
    public async Task Consume(ConsumeContext<CoinPriceEvent> context)
    {
        var message = context.Message;
        var cacheKey = $"{message.Symbol}:Orders";

        var limitOrders = await GetOrdersAsync(cacheKey, message.Symbol);
        if (limitOrders is not { Count: > 0 }) return;

        var triggered = limitOrders
            .Where(o => o is { OrderStatus: LimitOrderStatus.Pending } && IsTriggered(o, message.Price))
            .ToList();

        if (triggered.Count == 0) return;

        foreach (var order in triggered)
            order.OrderStatus = LimitOrderStatus.Processing; 

        await RefreshCacheAsync(cacheKey, limitOrders);

        await Parallel.ForEachAsync(triggered,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (order, ct) =>
            {
                logger.LogInformation(
                    "Applying {OrderType} order {OrderId} at {Price}",
                    order.OrderType, order.Id, message.Price);

                var dto = mapper.Map<ApplyLimitOrderDto>(order);
                await limitOrderService.ApplyLimitOrder(dto, message.Price);                          

                order.OrderStatus = LimitOrderStatus.Filled;
            });

        await RefreshCacheAsync(cacheKey, limitOrders);
    }


    private static bool IsTriggered(LimitOrder order, decimal currentPrice) =>
        order.OrderType switch
        {
            LimitOrderType.Buy => currentPrice <= order.TargetPrice,
            LimitOrderType.Sell => currentPrice >= order.TargetPrice,
            _ => false
        };

    private async Task<List<LimitOrder>> GetOrdersAsync(string key, string symbol)
    {
        var cached = await cacheService.GetAsync<List<LimitOrder>>(key);
        if (cached is not null) return cached;

        var orders = await limitOrderService.GetLimitOrdersBySymbol(symbol);
        await RefreshCacheAsync(key, orders);
        return orders;
    }

    private async Task RefreshCacheAsync(string key, List<LimitOrder> orders)
    {
        var dtos = mapper.Map<List<LimitOrderCacheDto>>(orders);
        await cacheService.SetAsync(key, dtos, TimeSpan.FromMinutes(2));
    }
}