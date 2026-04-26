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
    public async Task<AuthResult> AddUserAsync(string userName, string firstName, string lastName, string email, string password)
    {
        try
        {
            var addUser = new AppUser
            {
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
            };

            var result = await userManager.CreateAsync(addUser, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Register is unsuccessfull."
                };
            }

            await publishEndpoint.Publish(new UserCreatedEvent { Email = email, UserName = userName, UserId = addUser.Id });

            var jwtToken = await authenticationService.LoginAsync(addUser.Email, password);

            return new AuthResult
            {
                IsSuccess = jwtToken != null,
                Token = jwtToken,
                ErrorMessage = (jwtToken == null) ? "Register is successfull but could not create token." : string.Empty
            };
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.Message);

            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "Server error"
            };
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
