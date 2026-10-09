using MassTransit;
using Portfolio.API.Application.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class BuyCoinConsumer(IWalletService walletService) : IConsumer<BuyCoinEvent>
{
    public async Task Consume(ConsumeContext<BuyCoinEvent> context)
    {
        var message = context.Message;
        var isLimitOrder = false;
        await walletService.BuyAssetWithUserId(message.UserId.ToString(), message.Symbol, message.BuyPrice, message.BuyAmount, isLimitOrder);
    }
}
