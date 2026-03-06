using AutoMapper;
using Identity.API.Application.DTOs;
using Identity.API.Application.Interfaces;
using Identity.API.Domain.Entities;
using Identity.API.Domain.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Shared.Messages;

namespace Identity.API.Application.Services;

public class UserService(IUserRepository userRepository, IMapper mapper, IPublishEndpoint publishEndpoint, IAuthenticationService authenticationService, UserManager<AppUser> userManager) : IUserService
{
    public async Task<string> AddUserAsync(string userName, string email, string password)
    {
        try
        {
            var addUser = new AppUser
            {
                UserName = userName,
                Email = email
            };

            var result = await userManager.CreateAsync(addUser, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return "Register Unsuccessfull: " + errors;
            }

            await publishEndpoint.Publish(new UserCreatedEvent { Email = email, UserName = userName, UserId = addUser.Id });

            var jwtToken = await authenticationService.LoginAsync(addUser.Email, password);

            return jwtToken ?? "Register is successfull but could not create token.";
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.Message);
            return "Server Error: " + ex.Message;
        }
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        try
        {
            var user = await userRepository.GetByIdAsync(id);
            return mapper.Map<UserDto>(user);
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex.Message);
            return new UserDto();
        }
    }
}
