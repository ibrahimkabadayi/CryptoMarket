using AutoMapper;
using MassTransit;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Application.Services;

public class LimitOrderService(ILimitOrderRepository limitOrderRepository, IWalletService walletService, IPublishEndpoint publishEndpoint, IMapper mapper, ICacheService cacheService) : ILimitOrderService
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
                var result = await walletService.BuyAsset(limitOrder.WalletId, limitOrder.Symbol, price, limitOrder.Amount, true);
                Console.WriteLine(result);

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
            await walletService.SellAsset(limitOrder.WalletId, limitOrder.Symbol, price, limitOrder.Amount, true);

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

    public async Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto)
    {
        ArgumentNullException.ThrowIfNull(orderDto);

        var limitOrder = new LimitOrder(
            orderDto.WalletId,
            orderDto.UserId,
            orderDto.Symbol,
            orderDto.TargetPrice,
            orderDto.Amount,
            orderDto.OrderType
        );

        await limitOrderRepository.AddAsync(limitOrder);

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

    public async Task UpdateLimitOrderAsync(Guid limitOrderId, decimal? amount, decimal? targetPrice)
    {
        var limitOrder = await limitOrderRepository.GetByIdAsync(limitOrderId);
        if (limitOrder is null)
            throw new ArgumentException("Error: Could not found limit order.");

        limitOrder.Update(amount, targetPrice);
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
}
