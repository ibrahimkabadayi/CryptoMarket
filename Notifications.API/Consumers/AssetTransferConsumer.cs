using MassTransit;
using Shared.Messages;

namespace Notifications.API.Consumers;

public class AssetTransferConsumer : IConsumer<AssetTransferEvent>
{
    public Task Consume(ConsumeContext<AssetTransferEvent> context)
    {
        var message = context.Message;
        Console.WriteLine(message.Message);
        throw new NotImplementedException();
    }
}
