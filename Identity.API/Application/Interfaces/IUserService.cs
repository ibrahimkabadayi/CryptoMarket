using Identity.API.Application.DTOs;

namespace Identity.API.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<AuthResult> AddUserAsync(string userName, string firstName, string LastName, string email, string password);
}
