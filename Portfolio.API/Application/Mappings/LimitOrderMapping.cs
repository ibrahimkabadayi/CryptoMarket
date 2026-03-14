using AutoMapper;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Mappings;

public class LimitOrderMapping : Profile
{
    public LimitOrderMapping()
    {
        CreateMap<LimitOrderDto, LimitOrder>();
        CreateMap<LimitOrder, LimitOrderDto>();
    }
}
