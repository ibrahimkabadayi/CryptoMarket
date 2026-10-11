using FluentAssertions;
using Microsoft.Extensions.Options;
using Notifications.API.Application.Services;
using Notifications.API.Application.Settings;
using Xunit;

namespace CryptoMarket.UnitTests.Notifications.Services;

public class EmailServiceTests
{
    private readonly EmailService _sut;

    public EmailServiceTests()
    {
        var settings = new EmailSettings
        {
            Host = "127.0.0.1",
            Port = 2525,
            Mail = "no-reply@cryptomarket.com",
            DisplayName = "CryptoMarket Support",
            Password = "FakePassword123"
        };

        var options = Options.Create(settings);
        _sut = new EmailService(options);
    }

    [Fact]
    public async Task SendEmailAsync_WhenInvoked_ExecutesGracefullyWithoutUnhandledExceptions()
    {
        // Arrange
        const string toEmail = "recipient@example.com";
        const string subject = "Test Subject";
        const string body = "<p>Test Body</p>";

        // Act
        var act = async () => await _sut.SendEmailAsync(toEmail, subject, body, isHtml: true);

        // Assert - SendEmailAsync internally catches socket/connection errors
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenInvoked_ExecutesGracefullyWithoutUnhandledExceptions()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string name = "John Doe";

        // Act
        var act = async () => await _sut.SendWelcomeEmailAsync(email, name);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
