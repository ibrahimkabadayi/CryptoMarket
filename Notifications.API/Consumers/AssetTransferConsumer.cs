using MassTransit;
using Notifications.API.Application.Interfaces;
using Shared.Messages;

namespace Notifications.API.Consumers;

public class AssetTransferConsumer(INotificationService notificationService) : IConsumer<AssetTransferEvent>
{
    public async Task Consume(ConsumeContext<AssetTransferEvent> context)
    {
        var message = context.Message;

        await notificationService.CreateNotificationAsync(
            message.SourceWalletUserId,
            "Transfer Successfull!",
            $"You have successfully trasfered {message.Quantity} {message.Symbol}",
            Domain.Enums.NotificationType.AssetTransfer
        );
    }
}
