using System.Linq.Expressions;
using AutoMapper;
using CryptoMarket.UnitTests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Application.Services;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Enums;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Hubs;
using NSubstitute;
using Xunit;

namespace CryptoMarket.UnitTests.Notifications.Services;

public class PriceAlertServiceTests
{
    private readonly IPriceAlertRepository _priceAlertRepository;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IClientProxy _clientProxy;
    private readonly PriceAlertService _sut;

    public PriceAlertServiceTests()
    {
        _priceAlertRepository = Substitute.For<IPriceAlertRepository>();
        _mapper = Substitute.For<IMapper>();
        _notificationService = Substitute.For<INotificationService>();
        _cacheService = Substitute.For<ICacheService>();

        var mockHub = MockHubContext.Create<NotificationHub>();
        _hubContext = mockHub.HubContext;
        _clientProxy = mockHub.ClientProxy;

        _sut = new PriceAlertService(
            _priceAlertRepository,
            _mapper,
            _notificationService,
            _cacheService,
            _hubContext
        );
    }

    [Fact]
    public async Task CreateAlertAsync_WhenCalled_AddsAlertRemovesCacheSendsSignalRAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string symbol = "BTC";
        const decimal targetPrice = 65000m;
        const bool isAbove = true;

        var expectedDto = new PriceAlertDto(userId, symbol, targetPrice, isAbove);

        _mapper.Map<PriceAlertDto>(Arg.Any<PriceAlert>()).Returns(expectedDto);

        // Act
        var result = await _sut.CreateAlertAsync(userId.ToString(), symbol, targetPrice, isAbove);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);

        await _priceAlertRepository.Received(1).AddAsync(Arg.Is<PriceAlert>(a =>
            a.UserId == userId &&
            a.Symbol == symbol &&
            a.TargetPrice == targetPrice &&
            a.IsAbove == isAbove &&
            a.IsActive
        ));

        await _cacheService.Received(1).RemoveAsync("BTCAlerts");

        await _clientProxy.Received(1).SendCoreAsync(
            "ReceivePriceAlert",
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is PriceAlert),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetActiveAlertsByUserAsync_WhenCalled_ReturnsMappedActiveAlerts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alerts = new List<PriceAlert>
        {
            new(userId, "ETH", 3500m, true),
            new(userId, "SOL", 150m, false)
        };
        var expectedDtos = new List<PriceAlertDto>
        {
            new(userId, "ETH", 3500m, true),
            new(userId, "SOL", 150m, false)
        };

        _priceAlertRepository.FindAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>())
            .Returns(alerts);
        _mapper.Map<IEnumerable<PriceAlertDto>>(Arg.Any<IEnumerable<PriceAlert>>())
            .Returns(expectedDtos);

        // Act
        var result = await _sut.GetActiveAlertsByUserAsync(userId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDtos);
        await _priceAlertRepository.Received(1).FindAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>());
    }

    [Fact]
    public async Task GetAllAlertsByUserAsync_WhenCalled_ReturnsMappedAlerts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alerts = new List<PriceAlert>
        {
            new(userId, "BTC", 70000m, true)
        };
        var expectedDtos = new List<PriceAlertDto>
        {
            new(userId, "BTC", 70000m, true)
        };

        _priceAlertRepository.FindAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>())
            .Returns(alerts);
        _mapper.Map<IEnumerable<PriceAlertDto>>(Arg.Any<IEnumerable<PriceAlert>>())
            .Returns(expectedDtos);

        // Act
        var result = await _sut.GetAllAlertsByUserAsync(userId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDtos);
    }

    [Fact]
    public async Task DeactivateAlertAsync_WhenAlertExistsAndBelongsToUser_DeactivatesAndNotifiesSignalR()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var alert = new PriceAlert(userId, "BTC", 60000m, true);

        _priceAlertRepository.GetByIdAsync(alertId).Returns(alert);

        // Act
        await _sut.DeactivateAlertAsync(alertId, userId.ToString());

        // Assert
        alert.IsActive.Should().BeFalse();
        await _cacheService.Received(1).RemoveAsync("BTCAlerts");
        await _priceAlertRepository.Received(1).UpdateAsync(alert);
        await _clientProxy.Received(1).SendCoreAsync(
            "DeactivatePriceAlert",
            Arg.Is<object?[]>(args => args.Length == 1 && (Guid)args[0]! == alertId),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeactivateAlertAsync_WhenAlertNotFound_ThrowsException()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _priceAlertRepository.GetByIdAsync(alertId).Returns((PriceAlert?)null);

        // Act
        var act = async () => await _sut.DeactivateAlertAsync(alertId, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Alert not found or unauthorized access.");
        await _priceAlertRepository.DidNotReceive().UpdateAsync(Arg.Any<PriceAlert>());
    }

    [Fact]
    public async Task DeactivateAlertAsync_WhenAlertBelongsToDifferentUser_ThrowsException()
    {
        // Arrange
        var alertOwnerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var alert = new PriceAlert(alertOwnerId, "BTC", 60000m, true);

        _priceAlertRepository.GetByIdAsync(alertId).Returns(alert);

        // Act
        var act = async () => await _sut.DeactivateAlertAsync(alertId, anotherUserId.ToString());

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Alert not found or unauthorized access.");
        await _priceAlertRepository.DidNotReceive().UpdateAsync(Arg.Any<PriceAlert>());
    }

    [Fact]
    public async Task GetActiveAlertsBySymbolAsync_WhenCalled_ReturnsActiveAlerts()
    {
        // Arrange
        const string symbol = "BTC";
        var alerts = new List<PriceAlert>
        {
            new(Guid.NewGuid(), symbol, 60000m, true),
            new(Guid.NewGuid(), symbol, 62000m, false)
        };

        _priceAlertRepository.FindAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>())
            .Returns(alerts);

        // Act
        var result = await _sut.GetActiveAlertsBySymbolAsync(symbol);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(alerts);
    }

    [Fact]
    public async Task CheckPriceAlerts_WhenPriceRisesAboveTarget_TriggersNotificationAndDeactivates()
    {
        // Arrange
        const string symbol = "BTC";
        var userId = Guid.NewGuid();
        var alert = new PriceAlert(userId, symbol, 60000m, isAbove: true);
        var activeAlerts = new List<PriceAlert> { alert };

        _cacheService.GetAsync<List<PriceAlert>>("BTCAlerts").Returns(activeAlerts);
        _priceAlertRepository.GetByIdAsync(alert.Id).Returns(alert);

        // Act (Current price is 61000 >= 60000)
        await _sut.CheckPriceAlerts(symbol, 61000m);

        // Assert
        await _notificationService.Received(1).CreateNotificationAsync(
            userId.ToString(),
            $"{symbol} price alert!",
            Arg.Is<string>(msg => msg.Contains("rose above") && msg.Contains("61000")),
            NotificationType.PriceAlert,
            alert.Id.ToString()
        );

        alert.IsActive.Should().BeFalse();
        await _cacheService.Received(1).SetAsync("BTCAlerts", Arg.Any<List<PriceAlert>>(), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CheckPriceAlerts_WhenPriceFallsBelowTarget_TriggersNotificationAndDeactivates()
    {
        // Arrange
        const string symbol = "ETH";
        var userId = Guid.NewGuid();
        var alert = new PriceAlert(userId, symbol, 3000m, isAbove: false);
        var activeAlerts = new List<PriceAlert> { alert };

        _cacheService.GetAsync<List<PriceAlert>>("ETHAlerts").Returns(activeAlerts);
        _priceAlertRepository.GetByIdAsync(alert.Id).Returns(alert);

        // Act (Current price is 2900 <= 3000)
        await _sut.CheckPriceAlerts(symbol, 2900m);

        // Assert
        await _notificationService.Received(1).CreateNotificationAsync(
            userId.ToString(),
            $"{symbol} price alert!",
            Arg.Is<string>(msg => msg.Contains("fell blow") && msg.Contains("2900")),
            NotificationType.PriceAlert,
            alert.Id.ToString()
        );

        alert.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPriceAlerts_WhenPriceConditionNotMet_DoesNotTriggerNotification()
    {
        // Arrange
        const string symbol = "BTC";
        var userId = Guid.NewGuid();
        var alert = new PriceAlert(userId, symbol, 65000m, isAbove: true);
        var activeAlerts = new List<PriceAlert> { alert };

        _cacheService.GetAsync<List<PriceAlert>>("BTCAlerts").Returns(activeAlerts);

        // Act (Current price is 64000 < 65000)
        await _sut.CheckPriceAlerts(symbol, 64000m);

        // Assert
        await _notificationService.DidNotReceive().CreateNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<NotificationType>(),
            Arg.Any<string?>()
        );

        alert.IsActive.Should().BeTrue();
    }
}
