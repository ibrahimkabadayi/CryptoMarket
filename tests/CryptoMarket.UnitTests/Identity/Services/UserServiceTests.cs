using AutoMapper;
using CryptoMarket.UnitTests.Common.Helpers;
using FluentAssertions;
using Identity.API.Application.DTOs;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Services;
using Identity.API.Domain.Entities;
using Identity.API.Domain.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Messages;
using Xunit;

namespace CryptoMarket.UnitTests.Identity.Services;

public class UserServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAuthenticationService _authenticationService;
    private readonly UserManager<AppUser> _userManager;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _mapper = Substitute.For<IMapper>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _authenticationService = Substitute.For<IAuthenticationService>();
        _userManager = MockUserManager.Create<AppUser>();

        _sut = new UserService(
            _userRepository,
            _mapper,
            _publishEndpoint,
            _authenticationService,
            _userManager
        );
    }

    [Fact]
    public async Task AddUserAsync_WhenRegistrationSucceedsAndTokenGenerated_ReturnsSuccessfulAuthResultWithToken()
    {
        // Arrange
        const string userName = "johndoe";
        const string firstName = "John";
        const string lastName = "Doe";
        const string email = "john.doe@example.com";
        const string password = "SecurePassword123!";
        const string generatedToken = "jwt-test-token-value";

        _userManager.CreateAsync(Arg.Any<AppUser>(), password)
            .Returns(IdentityResult.Success);

        _authenticationService.LoginAsync(email, password)
            .Returns(generatedToken);

        // Act
        var result = await _sut.AddUserAsync(userName, firstName, lastName, email, password);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().Be(generatedToken);
        result.ErrorMessage.Should().BeEmpty();

        await _userManager.Received(1).CreateAsync(
            Arg.Is<AppUser>(u =>
                u.UserName == userName &&
                u.Email == email &&
                u.FirstName == firstName &&
                u.LastName == lastName),
            password
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserCreatedEvent>(e =>
                e.Email == email &&
                e.UserName == userName),
            Arg.Any<CancellationToken>()
        );

        await _authenticationService.Received(1).LoginAsync(email, password);
    }

    [Fact]
    public async Task AddUserAsync_WhenIdentityCreationFails_ReturnsFailureWithoutPublishingOrLoggingIn()
    {
        // Arrange
        const string userName = "janedoe";
        const string firstName = "Jane";
        const string lastName = "Doe";
        const string email = "jane.doe@example.com";
        const string password = "WeakPassword";

        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" }
        };

        _userManager.CreateAsync(Arg.Any<AppUser>(), password)
            .Returns(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _sut.AddUserAsync(userName, firstName, lastName, email, password);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Register is unsuccessfull.");
        result.Token.Should().BeEmpty();

        await _publishEndpoint.DidNotReceive().Publish(
            Arg.Any<UserCreatedEvent>(),
            Arg.Any<CancellationToken>()
        );
        await _authenticationService.DidNotReceive().LoginAsync(
            Arg.Any<string>(),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task AddUserAsync_WhenRegistrationSucceedsButTokenGenerationFails_ReturnsFailureWithSpecificErrorMessage()
    {
        // Arrange
        const string userName = "alexsmith";
        const string firstName = "Alex";
        const string lastName = "Smith";
        const string email = "alex.smith@example.com";
        const string password = "SecurePassword123!";

        _userManager.CreateAsync(Arg.Any<AppUser>(), password)
            .Returns(IdentityResult.Success);

        _authenticationService.LoginAsync(email, password)
            .Returns((string?)null);

        // Act
        var result = await _sut.AddUserAsync(userName, firstName, lastName, email, password);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ErrorMessage.Should().Be("Register is successfull but could not create token.");

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<UserCreatedEvent>(e => e.Email == email && e.UserName == userName),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task AddUserAsync_WhenExceptionOccurs_ReturnsServerErrorAuthResult()
    {
        // Arrange
        _userManager.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>())
            .ThrowsAsync(new Exception("Database connectivity failure"));

        // Act
        var result = await _sut.AddUserAsync("user", "First", "Last", "error@test.com", "pass");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Server error");
        result.Token.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsMappedUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var appUser = new AppUser
        {
            Id = userId,
            UserName = "johndoe",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var expectedDto = new UserDto
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com"
        };

        _userRepository.GetByIdAsync(userId).Returns(appUser);
        _mapper.Map<UserDto>(appUser).Returns(expectedDto);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        await _userRepository.Received(1).GetByIdAsync(userId);
        _mapper.Received(1).Map<UserDto>(appUser);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenExceptionOccurs_ReturnsEmptyUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId).ThrowsAsync(new Exception("Repository crashed"));

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeEmpty();
        result.Name.Should().BeEmpty();
    }
}
