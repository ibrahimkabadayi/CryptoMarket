using MassTransit;
using Portfolio.API.Application.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class BuyCoinConsumer(IWalletService walletService) : IConsumer<BuyCoinEvent>
{
    public async Task Consume(ConsumeContext<BuyCoinEvent> context)
    {
        var message = context.Message;
        var walletId = await walletService.GetWalletIdByUserId(message.UserId);

        await walletService.BuyAsset(walletId, message.Symbol, message.BuyPrice, message.BuyAmount, false);
    }
}
