using MassTransit;
using Notifications.API.Application.Interfaces;
using Shared.Messages;

namespace Notifications.API.Consumers;

public class UserCreatedConsumer(IEmailService emailService) : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        await emailService.SendWelcomeEmailAsync(message.Email, message.UserName);
    }
}
