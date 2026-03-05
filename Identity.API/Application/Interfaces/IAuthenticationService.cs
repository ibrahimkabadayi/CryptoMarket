using Identity.API.Application.DTOs;

namespace Identity.API.Application.Interfaces;

public interface IAuthenticationService
{
    public Task<string?> LoginAsync(string email, string password);
}
