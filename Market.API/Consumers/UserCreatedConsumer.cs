using MassTransit;
using Shared.Messages;

namespace Market.API.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"\n----New User Added! ID: {message.UserId}, Email: {message.Email}----");
        Console.WriteLine("----Creating an empty basket for new user...----");
        
        await Task.CompletedTask;
    }
}
