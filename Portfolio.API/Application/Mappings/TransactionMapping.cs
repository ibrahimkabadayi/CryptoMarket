using AutoMapper;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Mappings;

public class TransactionMapping : Profile
{
    public TransactionMapping()
    {
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.Wallet,
            opt => opt.MapFrom(src => src.Wallet));

        CreateMap<TransactionDto, Transaction>();
    }
}
