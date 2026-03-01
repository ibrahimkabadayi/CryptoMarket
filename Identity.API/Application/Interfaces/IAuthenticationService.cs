using Identity.API.Application.DTOs;

namespace Identity.API.Application.Interfaces;

public interface IAuthenticationService
{
    public Task LoginAsync(UserDto user, HttpContext context);
    public Task LogoutAsync();
}
