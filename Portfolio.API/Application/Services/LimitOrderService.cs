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
    public async Task<string> ApplyLimitOrder(ApplyLimitOrderDto limitOrder, decimal price)
    {
        if (limitOrder == null)
        {
            return "Error: Limit order is corrupted";
        }
        
        if(limitOrder.OrderType == LimitOrderType.Buy)
        {
            try
            {
                await walletService.BuyAsset(limitOrder.WalletId, limitOrder.Symbol, price, limitOrder.Amount);

              //  await publishEndpoint.Publish(new LimitOrderOccuredEvent 
               // { 
                //    Amount = limitOrder.Amount,
                 //   Symbol = limitOrder.Symbol,
                  //  Price = price,
                   // UserId = limitOrder.UserId,
                   // Ordertype = "Buy"
               // });                
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        else
        {
            await walletService.SellAsset(limitOrder.WalletId, limitOrder.Symbol, price, limitOrder.Amount);
            
            //await publishEndpoint.Publish(new LimitOrderOccuredEvent
            //{
            //    Amount = limitOrder.Amount,
            //    Symbol = limitOrder.Symbol,
            //    Price = price,
            //    UserId = limitOrder.UserId,
            //    Ordertype = "Sell"
            //});
        }

        await limitOrderRepository.UpdateAsync(limitOrder.Id, LimitOrderStatus.Filled);

        return "Success: Limit order applied";
    }

    public async Task CreateLimitOrderAsync(CreateLimitOrderDto orderDto)
    {
        ArgumentNullException.ThrowIfNull(orderDto);

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

    public async Task<string> UpdateLimitOrderAsync(Guid limitOrderId, decimal? Amount, decimal? TargetPrice)
    {
        var limitOrder = await limitOrderRepository.GetByIdAsync(limitOrderId);

        if (limitOrder == null) {
            return "Error: Could bot found limit order.";
        }

        if(Amount == null)
        {
            if(TargetPrice.HasValue)
                limitOrder.TargetPrice = (decimal)TargetPrice;
        }
        else if(TargetPrice is null)
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

        return "Success: Limit Order Updated";
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
