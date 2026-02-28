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

    public async Task<string> AddUserAsync(string userName, string email, string password)
    {
        try
        {
            var userExists = await _userRepository.FindFirstAsync(x => x.Email == email);
            if (userExists != null) 
            {
                await _publishEndpoint.Publish(new UserAlreadyRegistiredEvent { UserEmail = userExists.Email, UserName = userExists.Name });
                return "This user is already registired.";
            } 

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var addUser = new User
            {
                Name = userName,
                Email = email,
                PasswordHash = passwordHash
            };

            await _userRepository.AddAsync(addUser);

            await _publishEndpoint.Publish(new UserCreatedEvent { Email = email, UserName = userName, UserId = addUser.Id });
            return "User Added";
        }
        catch (Exception ex) 
        {
            Console.Beep();
            Console.WriteLine(ex.Message);
            return "Repository Error: " + ex.Message;
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
