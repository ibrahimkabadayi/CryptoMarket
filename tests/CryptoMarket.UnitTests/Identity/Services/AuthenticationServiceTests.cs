using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CryptoMarket.UnitTests.Common.Helpers;
using FluentAssertions;
using Identity.API.Application.Services;
using Identity.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace CryptoMarket.UnitTests.Identity.Services;

public class AuthenticationServiceTests
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AuthenticationService _sut;

    private const string ValidKey = "super_secret_jwt_key_that_is_at_least_32_characters_long_123456";
    private const string ValidIssuer = "CryptoMarketIssuer";
    private const string ValidAudience = "CryptoMarketAudience";
    private const string ValidExpireMinutes = "60";

    public AuthenticationServiceTests()
    {
        _userManager = MockUserManager.Create<AppUser>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", ValidKey },
            { "Jwt:Issuer", ValidIssuer },
            { "Jwt:Audience", ValidAudience },
            { "Jwt:ExpireMinutes", ValidExpireMinutes }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _sut = new AuthenticationService(_userManager, _configuration);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        const string email = "nonexistent@example.com";
        const string password = "Password123!";

        _userManager.FindByEmailAsync(email).Returns((AppUser?)null);

        // Act
        var result = await _sut.LoginAsync(email, password);

        // Assert
        result.Should().BeNull();
        await _userManager.Received(1).FindByEmailAsync(email);
        await _userManager.DidNotReceive().CheckPasswordAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ReturnsNull()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "WrongPassword!";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "testuser"
        };

        _userManager.FindByEmailAsync(email).Returns(user);
        _userManager.CheckPasswordAsync(user, password).Returns(false);

        // Act
        var result = await _sut.LoginAsync(email, password);

        // Assert
        result.Should().BeNull();
        await _userManager.Received(1).FindByEmailAsync(email);
        await _userManager.Received(1).CheckPasswordAsync(user, password);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsValidJwtToken()
    {
        // Arrange
        const string email = "validuser@example.com";
        const string password = "CorrectPassword123!";
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Email = email,
            UserName = "validuser"
        };

        _userManager.FindByEmailAsync(email).Returns(user);
        _userManager.CheckPasswordAsync(user, password).Returns(true);

        // Act
        var tokenString = await _sut.LoginAsync(email, password);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.CanReadToken(tokenString).Should().BeTrue();

        var jwtToken = tokenHandler.ReadJwtToken(tokenString);
        jwtToken.Issuer.Should().Be(ValidIssuer);
        jwtToken.Audiences.Should().Contain(ValidAudience);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwtToken.Claims.Should().Contain(c => (c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier) && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}
