using MassTransit;
using Shared.Messages;

namespace Market.API.Consumers;

public class UserAlreadyRegistiredConsumer : IConsumer<UserAlreadyRegistiredEvent>
{
    public Task Consume(ConsumeContext<UserAlreadyRegistiredEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"The user with email {message.UserEmail} has already registired");

        return Task.CompletedTask;
    }
}
