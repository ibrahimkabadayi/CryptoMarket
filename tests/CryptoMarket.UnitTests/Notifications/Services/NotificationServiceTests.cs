using System.Linq.Expressions;
using AutoMapper;
using CryptoMarket.UnitTests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Services;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Enums;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Hubs;
using NSubstitute;
using Xunit;

namespace CryptoMarket.UnitTests.Notifications.Services;

public class NotificationServiceTests
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMapper _mapper;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IClientProxy _clientProxy;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _notificationRepository = Substitute.For<INotificationRepository>();
        _mapper = Substitute.For<IMapper>();

        var mockHub = MockHubContext.Create<NotificationHub>();
        _hubContext = mockHub.HubContext;
        _clientProxy = mockHub.ClientProxy;

        _sut = new NotificationService(_notificationRepository, _mapper, _hubContext);
    }

    [Fact]
    public async Task CreateNotificationAsync_WhenValidInput_AddsNotificationAndSendsSignalRMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string title = "Order Filled";
        const string message = "Your BTC buy order has been filled.";
        const NotificationType type = NotificationType.LimitOrderMatch;
        const string relatedEntityId = "order-123";

        // Act
        await _sut.CreateNotificationAsync(userId.ToString(), title, message, type, relatedEntityId);

        // Assert
        await _notificationRepository.Received(1).AddAsync(Arg.Is<Notification>(n =>
            n.UserId == userId &&
            n.Title == title &&
            n.Message == message &&
            n.Type == type &&
            n.RelatedEntityId == relatedEntityId &&
            !n.IsRead
        ));

        await _clientProxy.Received(1).SendCoreAsync(
            "ReceiveNotification",
            Arg.Is<object?[]>(args => args.Length == 1 && args[0] is Notification && ((Notification)args[0]!).UserId == userId),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WhenCalled_ReturnsMappedNotificationsOrderedDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new(userId, "Title 1", "Message 1", NotificationType.GeneralInfo),
            new(userId, "Title 2", "Message 2", NotificationType.PriceAlert)
        };

        var expectedDtos = new List<NotificationDto>
        {
            new(userId, "Title 2", "Message 2", NotificationType.PriceAlert),
            new(userId, "Title 1", "Message 1", NotificationType.GeneralInfo)
        };

        _notificationRepository.FindAsync(Arg.Any<Expression<Func<Notification, bool>>>())
            .Returns(notifications);

        _mapper.Map<IEnumerable<NotificationDto>>(Arg.Any<IEnumerable<Notification>>())
            .Returns(expectedDtos);

        // Act
        var result = await _sut.GetUserNotificationsAsync(userId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDtos);
        await _notificationRepository.Received(1).FindAsync(Arg.Any<Expression<Func<Notification, bool>>>());
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenCalled_ReturnsCountOfUnreadNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var unreadNotifications = new List<Notification>
        {
            new(userId, "Title 1", "Message 1", NotificationType.GeneralInfo),
            new(userId, "Title 2", "Message 2", NotificationType.PriceAlert)
        };

        _notificationRepository.FindAsync(Arg.Any<Expression<Func<Notification, bool>>>())
            .Returns(unreadNotifications);

        // Act
        var count = await _sut.GetUnreadCountAsync(userId.ToString());

        // Assert
        count.Should().Be(2);
        await _notificationRepository.Received(1).FindAsync(Arg.Any<Expression<Func<Notification, bool>>>());
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExistsAndBelongsToUser_MarksAsReadAndNotifiesSignalR()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var notification = new Notification(userId, "Title", "Message", NotificationType.GeneralInfo);

        _notificationRepository.GetByIdAsync(notificationId).Returns(notification);

        // Act
        await _sut.MarkAsReadAsync(notificationId, userId.ToString());

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();

        await _notificationRepository.Received(1).UpdateAsync(notification);
        await _clientProxy.Received(1).SendCoreAsync(
            "DeactivateNotification",
            Arg.Is<object?[]>(args => args.Length == 1 && (string)args[0]! == notification.Id.ToString()),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationNotFound_ThrowsException()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _notificationRepository.GetByIdAsync(notificationId).Returns((Notification?)null);

        // Act
        var act = async () => await _sut.MarkAsReadAsync(notificationId, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Bildirim bulunamadı veya yetkisiz erişim.");
        await _notificationRepository.DidNotReceive().UpdateAsync(Arg.Any<Notification>());
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationBelongsToDifferentUser_ThrowsException()
    {
        // Arrange
        var notificationOwnerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var notification = new Notification(notificationOwnerId, "Title", "Message", NotificationType.GeneralInfo);

        _notificationRepository.GetByIdAsync(notificationId).Returns(notification);

        // Act
        var act = async () => await _sut.MarkAsReadAsync(notificationId, anotherUserId.ToString());

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Bildirim bulunamadı veya yetkisiz erişim.");
        await _notificationRepository.DidNotReceive().UpdateAsync(Arg.Any<Notification>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenCalled_MarksAllUnreadNotificationsAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification1 = new Notification(userId, "Title 1", "Msg 1", NotificationType.GeneralInfo);
        var notification2 = new Notification(userId, "Title 2", "Msg 2", NotificationType.PriceAlert);
        var unreadList = new List<Notification> { notification1, notification2 };

        _notificationRepository.FindAsync(Arg.Any<Expression<Func<Notification, bool>>>())
            .Returns(unreadList);

        // Act
        await _sut.MarkAllAsReadAsync(userId.ToString());

        // Assert
        notification1.IsRead.Should().BeTrue();
        notification2.IsRead.Should().BeTrue();

        await _notificationRepository.Received(1).UpdateAsync(notification1);
        await _notificationRepository.Received(1).UpdateAsync(notification2);
    }
}
