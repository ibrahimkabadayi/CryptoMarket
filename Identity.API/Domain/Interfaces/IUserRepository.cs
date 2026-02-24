using Identity.API.Application.DTOs;
using Identity.API.Domain.Entities;

namespace Identity.API.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task AddAsync(UserDto addUser);
}