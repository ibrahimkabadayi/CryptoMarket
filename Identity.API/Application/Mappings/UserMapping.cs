using AutoMapper;
using Identity.API.Application.DTOs;
using Identity.API.Domain.Entities;

namespace Identity.API.Application.Mappings;

public class UserMapping : Profile
{
    public UserMapping() 
    {
        CreateMap<UserDto, User>();

        CreateMap<User, UserDto>();
    }
}
