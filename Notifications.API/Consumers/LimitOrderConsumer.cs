using MassTransit;
using Notifications.API.Application.Interfaces;
using Shared.Messages;

namespace Notifications.API.Consumers;

public class LimitOrderConsumer(INotificationService notificationService) : IConsumer<LimitOrderOccuredEvent>
{
    public async Task Consume(ConsumeContext<LimitOrderOccuredEvent> context)
    {
        var message = context.Message;

        await notificationService.CreateNotificationAsync(
            message.UserId,
            "Your Limit Order occured",
            $"Target Price of {message.Price} is reached\nYou have bought {message.Amount} of {message.Symbol}",
            Domain.Enums.NotificationType.LimitOrderMatch);

        return;
    }
}
