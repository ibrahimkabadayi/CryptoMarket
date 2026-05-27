using AutoMapper;
using Market.API.Application.DTOs;
using Market.API.Domain.Entities;

namespace Market.API.Application.Mappings;

public class MarketNewsMapping : Profile
{
    public MarketNewsMapping()
    {
        CreateMap<MarketNewsDto, MarketNews>();
        CreateMap<MarketNews, MarketNewsDto>();
    }
}
