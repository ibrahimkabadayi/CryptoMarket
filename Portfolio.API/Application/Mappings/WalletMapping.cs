using AutoMapper;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Mappings;

public class WalletMapping : Profile
{
    public WalletMapping()
    {
        CreateMap<Wallet, WalletDto>()
            .ForMember(dest => dest.Assets,
                opt => opt.MapFrom(
                    src => src.Assets))
            .ForMember(dest => dest.Transactions,
                opt => opt.MapFrom(
                    src => src.Transactions));

        CreateMap<WalletDto, Wallet>();
    }
}
