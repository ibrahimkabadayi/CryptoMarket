using AutoMapper;
using Market.API.Application.DTOs;
using Market.API.Domain.Entities;

namespace Market.API.Application.Mappings;

public class CoinMapping : Profile
{
    public CoinMapping()
    {
        CreateMap<CoinDto, Coin>();
        CreateMap<Coin, CoinDto>();
    }
}
