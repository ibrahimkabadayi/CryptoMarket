using Market.API.Application.Interfaces;
using MassTransit;
using Shared.Messages;

namespace Market.API.Application.Services;

public class LimitOrderService(IPublishEndpoint publishEndpoint) : ILimitOrderService
{
    public async Task SetLimitOrder(string symbol, Guid userId, decimal targetPrice, decimal amount, string orderType)
    {
        try
        {
            await publishEndpoint.Publish(new LimitOrderPlacedEvent
            {
                UserId = userId,
                Symbol = symbol,
                TargetPrice = targetPrice,
                Amount = amount,
                OrderType = orderType
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }
    }
}
