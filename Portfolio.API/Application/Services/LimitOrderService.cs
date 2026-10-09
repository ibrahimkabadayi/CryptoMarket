using AutoMapper;
using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Application.Services;

public class LimitOrderService(
    ILimitOrderRepository limitOrderRepository,
    IWalletService walletService,
    IPublishEndpoint publishEndpoint,
    IMapper mapper, ICacheService cacheService
    ) : ILimitOrderService
{
    public async Task ApplyLimitOrder(ApplyLimitOrderDto limitOrder, decimal price)
    {
        if (limitOrder == null)
        {
            throw new ArgumentException("Error: Limit order is corrupted");
        }

        price = Math.Round(price, 4);

        Console.WriteLine($"WalletId: {limitOrder.WalletId}\nSymbol:{limitOrder.Symbol}\nCurrentPrice:{price}");


        if (limitOrder.OrderType == LimitOrderType.Buy)
        {
            try
            {
                await walletService.BuyAsset(limitOrder.UserId.ToString(), limitOrder.Symbol, price, limitOrder.Amount, true);        

                await publishEndpoint.Publish(new LimitOrderOccuredEvent
                {
                    Amount = limitOrder.Amount,
                    Symbol = limitOrder.Symbol,
                    Price = price,
                    UserId = limitOrder.UserId,
                    Ordertype = "Buy"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new ArgumentException("Error: " + ex.Message);
            }
        }
        else
        {
            await walletService.SellAsset(limitOrder.UserId.ToString(), limitOrder.Symbol, price, limitOrder.Amount, true);

            await publishEndpoint.Publish(new LimitOrderOccuredEvent
            {
                Amount = limitOrder.Amount,
                Symbol = limitOrder.Symbol,
                Price = price,
                UserId = limitOrder.UserId,
                Ordertype = "Sell"
            });
        }

        await limitOrderRepository.UpdateAsync(limitOrder.Id, LimitOrderStatus.Filled); 
    }

    public async Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto, Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(orderDto);

        var correlationIdStringValue = correlationId.ToString();

        var result = await cacheService.GetAsync<string>(correlationIdStringValue);

        if (result != null)
        {
            throw new InvalidOperationException("This order already done");
        }

        var limitOrder = new LimitOrder
        {
            Symbol = orderDto.Symbol,
            TargetPrice = orderDto.TargetPrice,
            WalletId = orderDto.WalletId,
            OrderType = orderDto.OrderType,
            Amount = orderDto.Amount,
            UserId = orderDto.UserId,
        };

        await limitOrderRepository.AddAsync(limitOrder);

        await cacheService.SetAsync<string>(correlationIdStringValue, correlationIdStringValue, TimeSpan.FromHours(24));

        var key = $"{orderDto.Symbol}Orders";
        await cacheService.RemoveAsync(key);
    }

    public async Task DeleteLimitOrderAsync(Guid limitOrderId)
    {
        await limitOrderRepository.DeleteAsync(limitOrderId);
    }

    public async Task<LimitOrderDto> GetLimitOrder(Guid id)
    {
        var limitOrder = await limitOrderRepository.GetByIdAsync(id);
        return mapper.Map<LimitOrderDto>(limitOrder);
    }

    public async Task<List<LimitOrderDto>> GetAllLimitOrders()
    {
        var limitOrders = await limitOrderRepository.GetAllAsync();
        return mapper.Map<List<LimitOrderDto>>(limitOrders);
    }

    public async Task UpdateLimitOrderAsync(Guid limitOrderId, decimal? Amount, decimal? TargetPrice)
    {
        var limitOrder = await limitOrderRepository.GetByIdAsync(limitOrderId) ?? throw new ArgumentException("Error: Could bot found limit order.");
        if (Amount == null)
        {
            if (TargetPrice.HasValue)
                limitOrder.TargetPrice = (decimal)TargetPrice;
        }
        else if (TargetPrice is null)
        {
            if (Amount.HasValue)
                limitOrder.Amount = (decimal)Amount;
        }
        else
        {
            limitOrder.TargetPrice = (decimal)TargetPrice;
            limitOrder.Amount = (decimal)Amount;
        }

        limitOrder.UpdatedDate = DateTime.UtcNow;
        await limitOrderRepository.UpdateAsync(limitOrder);
    }

    public async Task<List<LimitOrder>> GetLimitOrdersBySymbol(string symbol)
    {
        var key = $"{symbol}Orders";

        var limitOrders = await cacheService.GetAsync<List<LimitOrder>>(key);

        if (limitOrders is not null)
        {
            return limitOrders;
        }

        limitOrders = await limitOrderRepository.FindAsync(x => x.Symbol == symbol);

        await cacheService.SetAsync(key, limitOrders, TimeSpan.FromSeconds(5));

        return limitOrders;
    }

    public async Task CheckLimitOrders(string symbol, decimal price)
    {
        var cacheKey = $"{symbol}:Orders";

        var limitOrders = await GetOrdersAsync(cacheKey, symbol);
        if (limitOrders is not { Count: > 0 }) return;

        var triggered = limitOrders
            .Where(o => o is { OrderStatus: LimitOrderStatus.Pending } && IsTriggered(o, price))
            .ToList();

        if (triggered.Count == 0) return;

        foreach (var order in triggered)
            order.OrderStatus = LimitOrderStatus.Processing;

        await RefreshCacheAsync(cacheKey, limitOrders);

        await Parallel.ForEachAsync(triggered,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (order, ct) =>
            {
                Console.WriteLine($"Applying {order.OrderType} order {order.Id} at {price}");

                var dto = mapper.Map<ApplyLimitOrderDto>(order);
                await ApplyLimitOrder(dto, price);

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

        var orders = await GetLimitOrdersBySymbol(symbol);
        await RefreshCacheAsync(key, orders);
        return orders;
    }

    private async Task RefreshCacheAsync(string key, List<LimitOrder> orders)
    {
        var dtos = mapper.Map<List<LimitOrderCacheDto>>(orders);
        await cacheService.SetAsync(key, dtos, TimeSpan.FromMinutes(2));
    }
}
