using AutoMapper;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Domain.Entities;

namespace Portfolio.API.Application.Mappings;

public class AssetMapping : Profile
{
    public AssetMapping()
    {
        CreateMap<Asset, AssetDto>();

        CreateMap<AssetDto, Asset>();
    }
}
