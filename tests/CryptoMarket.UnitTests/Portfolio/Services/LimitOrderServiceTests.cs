using AutoMapper;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Services;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Domain.Interfaces;
using Shared.Messages;
using Xunit;

namespace CryptoMarket.UnitTests.Portfolio.Services;

public class LimitOrderServiceTests
{
    private readonly ILimitOrderRepository _limitOrderRepository;
    private readonly IWalletService _walletService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly LimitOrderService _sut;

    public LimitOrderServiceTests()
    {
        _limitOrderRepository = Substitute.For<ILimitOrderRepository>();
        _walletService = Substitute.For<IWalletService>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _mapper = Substitute.For<IMapper>();
        _cacheService = Substitute.For<ICacheService>();

        _sut = new LimitOrderService(
            _limitOrderRepository,
            _walletService,
            _publishEndpoint,
            _mapper,
            _cacheService
        );
    }

    [Fact]
    public async Task CreateLimitOrderAsync_WhenOrderIsNew_AddsToRepositoryAndSetsCache()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var dto = new CreateLimitOrderDto
        {
            WalletId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Symbol = "BTC",
            TargetPrice = 60000m,
            Amount = 0.5m,
            OrderType = LimitOrderType.Buy
        };

        _cacheService.GetAsync<string>(correlationId.ToString())
            .Returns((string?)null);

        // Act
        await _sut.CreateLimitOrderAsync(dto, correlationId);

        // Assert
        await _limitOrderRepository.Received(1).AddAsync(Arg.Is<LimitOrder>(o =>
            o.Symbol == dto.Symbol &&
            o.TargetPrice == dto.TargetPrice &&
            o.Amount == dto.Amount &&
            o.OrderType == dto.OrderType
        ));

        await _cacheService.Received(1).SetAsync(
            correlationId.ToString(),
            correlationId.ToString(),
            TimeSpan.FromHours(24)
        );

        await _cacheService.Received(1).RemoveAsync("BTCOrders");
    }

    [Fact]
    public async Task CreateLimitOrderAsync_WhenOrderAlreadyExistsInCache_ThrowsInvalidOperationException()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var dto = new CreateLimitOrderDto { Symbol = "BTC" };

        _cacheService.GetAsync<string>(correlationId.ToString())
            .Returns("existing-order-marker");

        // Act
        var act = async () => await _sut.CreateLimitOrderAsync(dto, correlationId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This order already done");
        await _limitOrderRepository.DidNotReceive().AddAsync(Arg.Any<LimitOrder>());
    }

    [Fact]
    public async Task ApplyLimitOrder_WhenBuyOrder_CallsWalletBuyPublishesEventAndFillsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var applyDto = new ApplyLimitOrderDto
        {
            Id = orderId,
            UserId = userId,
            WalletId = walletId,
            Symbol = "BTC",
            Amount = 1m,
            OrderType = LimitOrderType.Buy
        };
        const decimal executionPrice = 61000m;

        // Act
        await _sut.ApplyLimitOrder(applyDto, executionPrice);

        // Assert
        await _walletService.Received(1).BuyAsset(
            userId.ToString(),
            "BTC",
            executionPrice,
            1m,
            true
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<LimitOrderOccuredEvent>(e =>
                e.Symbol == "BTC" &&
                e.Amount == 1m &&
                e.Price == executionPrice &&
                e.UserId == userId &&
                e.Ordertype == "Buy"
            ),
            Arg.Any<CancellationToken>()
        );

        await _limitOrderRepository.Received(1).UpdateAsync(orderId, LimitOrderStatus.Filled);
    }

    [Fact]
    public async Task ApplyLimitOrder_WhenSellOrder_CallsWalletSellPublishesEventAndFillsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var applyDto = new ApplyLimitOrderDto
        {
            Id = orderId,
            UserId = userId,
            WalletId = walletId,
            Symbol = "ETH",
            Amount = 2.5m,
            OrderType = LimitOrderType.Sell
        };
        const decimal executionPrice = 3200m;

        // Act
        await _sut.ApplyLimitOrder(applyDto, executionPrice);

        // Assert
        await _walletService.Received(1).SellAsset(
            userId.ToString(),
            "ETH",
            executionPrice,
            2.5m,
            true
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<LimitOrderOccuredEvent>(e =>
                e.Symbol == "ETH" &&
                e.Amount == 2.5m &&
                e.Price == executionPrice &&
                e.UserId == userId &&
                e.Ordertype == "Sell"
            ),
            Arg.Any<CancellationToken>()
        );

        await _limitOrderRepository.Received(1).UpdateAsync(orderId, LimitOrderStatus.Filled);
    }

    [Fact]
    public async Task ApplyLimitOrder_WhenOrderIsNull_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _sut.ApplyLimitOrder(null!, 50000m);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Error: Limit order is corrupted");
    }

    [Fact]
    public async Task DeleteLimitOrderAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        await _sut.DeleteLimitOrderAsync(orderId);

        // Assert
        await _limitOrderRepository.Received(1).DeleteAsync(orderId);
    }

    [Fact]
    public async Task UpdateLimitOrderAsync_WhenOrderFound_UpdatesAmountAndTargetPrice()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new LimitOrder
        {
            Id = orderId,
            Amount = 1m,
            TargetPrice = 50000m
        };

        _limitOrderRepository.GetByIdAsync(orderId).Returns(existingOrder);

        // Act
        await _sut.UpdateLimitOrderAsync(orderId, 2m, 55000m);

        // Assert
        existingOrder.Amount.Should().Be(2m);
        existingOrder.TargetPrice.Should().Be(55000m);
        existingOrder.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        await _limitOrderRepository.Received(1).UpdateAsync(existingOrder);
    }
}
