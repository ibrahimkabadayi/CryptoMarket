using MassTransit;
using Portfolio.API.Application.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class FeeCollectionConsumer(ITreasureBalanceService treasureBalanceService) : IConsumer<FeeCollectionEvent>
{
    public async Task Consume(ConsumeContext<FeeCollectionEvent> context)
    {
        var message = context.Message;
        await treasureBalanceService.AddFee(message.Symbol, message.FeeAmount);
    }
}
