using AutoMapper;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;
using Portfolio.API.Domain.Enums;
using Shared.Messages;

namespace Portfolio.API.Application.Mappings;

public class LimitOrderMapping : Profile
{
    public LimitOrderMapping()
    {
        CreateMap<LimitOrderDto, LimitOrder>();

        CreateMap<LimitOrder, LimitOrderDto>();

        CreateMap<ApplyLimitOrderDto, LimitOrder>();
        CreateMap<LimitOrder, ApplyLimitOrderDto>();

        CreateMap<LimitOrder, LimitOrderCacheDto>().ReverseMap();

        CreateMap<LimitOrderPlacedEvent, CreateLimitOrderDto>()
            .ForMember(d => d.OrderType, o => o.MapFrom(s => ParseOrderType(s.OrderType)))
            .ForMember(d => d.WalletId, o => o.Ignore());
            
    }

    private static LimitOrderType ParseOrderType(string value) =>
        Enum.TryParse<LimitOrderType>(value, ignoreCase: true, out var t) && Enum.IsDefined(t)
            ? t
            : throw new ArgumentException($"Invalid order type: {value}");
}
