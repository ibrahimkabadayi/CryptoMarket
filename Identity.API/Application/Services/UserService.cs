using AutoMapper;
using Identity.API.Application.DTOs;
using Identity.API.Application.Interfaces;
using Identity.API.Domain.Entities;
using Identity.API.Domain.Interfaces;
using MassTransit;
using Shared.Messages;

namespace Identity.API.Application.Services;

public class UserService(IUserRepository userRepository, IMapper mapper, IPublishEndpoint publishEndpoint) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMapper _mapper = mapper;
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public async Task AddUserAsync(UserDto user)
    {
        try
        {
            var addUser = _mapper.Map<User>(user);
            await _userRepository.AddAsync(addUser);

            await _publishEndpoint.Publish(new UserCreatedEvent { Email = user.Email, UserName = addUser.Name, UserId = addUser.Id });
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            return _mapper.Map<UserDto>(user);
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex.Message);
            return new UserDto();
        }
    }
}
