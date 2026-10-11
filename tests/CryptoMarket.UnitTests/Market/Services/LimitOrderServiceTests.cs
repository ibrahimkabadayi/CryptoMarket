using FluentAssertions;
using Market.API.Application.Services;
using MassTransit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Messages;
using Xunit;

namespace CryptoMarket.UnitTests.Market.Services;

public class LimitOrderServiceTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly LimitOrderService _sut;

    public LimitOrderServiceTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _sut = new LimitOrderService(_publishEndpoint);
    }

    [Fact]
    public async Task SetLimitOrder_WhenCalled_PublishesLimitOrderPlacedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string symbol = "BTC";
        const decimal targetPrice = 64000m;
        const decimal amount = 1.2m;
        const string orderType = "Buy";

        // Act
        await _sut.SetLimitOrder(symbol, userId, targetPrice, amount, orderType);

        // Assert
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<LimitOrderPlacedEvent>(e =>
                e.UserId == userId &&
                e.Symbol == symbol &&
                e.TargetPrice == targetPrice &&
                e.Amount == amount &&
                e.OrderType == orderType
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SetLimitOrder_WhenPublishThrows_CatchesExceptionAndReturnsGracefully()
    {
        // Arrange
        _publishEndpoint.Publish(Arg.Any<LimitOrderPlacedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("RabbitMQ connection dropped"));

        // Act
        var act = async () => await _sut.SetLimitOrder("ETH", Guid.NewGuid(), 3000m, 2m, "Sell");

        // Assert
        await act.Should().NotThrowAsync();
    }
}
