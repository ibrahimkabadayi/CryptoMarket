using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Identity.API.Controllers;
using Identity.API.Models;
using Identity.API.Application.Interfaces;
using Identity.API.Application.DTOs;
using Microsoft.AspNetCore.Identity.Data;

namespace Identity.API.Tests;

public class AuthControllerTests
{
    class StubUserService : IUserService
    {
        public Func<Guid, Task<UserDto>?>? GetByIdAsync { get; set; }
        public Func<string, string, string, string, string, Task<string>>? AddUserAsyncImpl { get; set; }

       public Task<UserDto> GetUserByIdAsync(Guid id)
        {
            if (GetByIdAsync != null)
            {
                var t = GetByIdAsync(id);
                return t ?? Task.FromResult(new UserDto());
            }
            return Task.FromResult(new UserDto());
        }

        public Task<string> AddUserAsync(string userName, string firstName, string LastName, string email, string password)
        {
            if (AddUserAsyncImpl != null) return AddUserAsyncImpl(userName, firstName, LastName, email, password);
            return Task.FromResult<string?>(null!);
        }
    }

    class StubAuthService : IAuthenticationService
    {
        public Func<string, string, Task<string?>>? LoginImpl { get; set; }
        public Task<string?> LoginAsync(string email, string password) => LoginImpl != null ? LoginImpl(email, password) : Task.FromResult<string?>(null);
    }

    [Fact]
    public async Task Register_Returns_Ok_When_Service_Returns_Token()
    {
        var userService = new StubUserService
        {
            AddUserAsyncImpl = (u, f, l, e, p) => Task.FromResult("eyJ.fake.token")
        };
        var authService = new StubAuthService();
        var controller = new AuthController(userService, authService);
        var req = new RegisterUserRequest { UserName = "u", FirstName = "f", LastName = "l", Email = "e", Password = "p" };

        var result = await controller.Register(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        dynamic d = ok.Value!;
        Assert.Equal("eyJ.fake.token", (string)d.Token);
    }

    [Fact]
    public async Task GetUser_Returns_Ok_When_Service_Returns_UserDto()
    {
        var expected = new UserDto { Name = "Test", Email = "t@t", PasswordHash = "h" };
        var userService = new StubUserService
        {
            GetByIdAsync = id => Task.FromResult(expected)
        };
        var authService = new StubAuthService();
        var controller = new AuthController(userService, authService);

        var result = await controller.GetUser(Guid.NewGuid());
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<UserDto>(ok.Value!);
        Assert.Equal(expected.Email, returned.Email);
    }

    [Fact]
    public async Task Register_Returns_BadRequest_When_Service_Returns_Error()
    {
        var userService = new StubUserService
        {
            AddUserAsyncImpl = (u, f, l, e, p) => Task.FromResult("Email is already in use.")
        };
        var authService = new StubAuthService();
        var controller = new AuthController(userService, authService);
        var req = new RegisterUserRequest { UserName = "u", FirstName = "f", LastName = "l", Email = "e", Password = "p" };

        var result = await controller.Register(req);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        dynamic d = badRequest.Value!;
        Assert.Equal("Email is already in use.", (string)d.Error);
    }

    [Fact]
    public async Task Login_Returns_Ok_When_Service_Returns_Token()
    {
        var userService = new StubUserService();
        var authService = new StubAuthService
        {
            LoginImpl = (e, p) => Task.FromResult<string?>("eyJ.fake.login.token")
        };
        var controller = new AuthController(userService, authService);
        var req = new LoginRequest { Email = "e@e", Password = "p" };

        var result = await controller.Login(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        dynamic d = ok.Value!;
        Assert.Equal("eyJ.fake.login.token", (string)d.Token);
    }

    [Fact]
    public async Task Login_Returns_Unauthorized_When_Service_Returns_Null()
    {
        var userService = new StubUserService();
        var authService = new StubAuthService
        {
            LoginImpl = (e, p) => Task.FromResult<string?>(null)
        };
        var controller = new AuthController(userService, authService);
        var req = new LoginRequest { Email = "e@e", Password = "p" };

        var result = await controller.Login(req);
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("E-posta veya şifre hatalı.", unauthorized.Value);
    }

    [Fact]
    public async Task GetUser_Returns_NotFound_When_Service_Returns_Null()
    {
        var userService = new StubUserService
        {
            GetByIdAsync = id => Task.FromResult<UserDto>(null!)
        };
        var authService = new StubAuthService();
        var controller = new AuthController(userService, authService);

        var result = await controller.GetUser(Guid.NewGuid());
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        dynamic d = notFound.Value!;
        Assert.Equal("Could not found user.", (string)d.Error);
    }
}