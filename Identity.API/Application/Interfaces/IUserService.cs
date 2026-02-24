using Identity.API.Application.DTOs;

namespace Identity.API.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(Guid id);
        Task AddUserAsync(UserDto user);
    }
}
