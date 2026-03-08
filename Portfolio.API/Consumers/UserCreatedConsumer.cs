using MassTransit;
using Portfolio.API.Application.Interfaces;
using Shared.Messages;

namespace Portfolio.API.Consumers;

public class UserCreatedConsumer(IWalletService walletService) : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;
        var userId = message.UserId;

        var result = await walletService.CreateWallet(userId);

        Console.WriteLine($"Created new wallet at the address {result}!");
    }
}