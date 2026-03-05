using Identity.API.Application.DTOs;

namespace Identity.API.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<string> AddUserAsync(string userName, string email, string password);
}
