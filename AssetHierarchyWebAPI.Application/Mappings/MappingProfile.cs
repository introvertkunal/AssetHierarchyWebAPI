using AutoMapper;
using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Domain.Entities;

namespace AssetHierarchyWebAPI.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AssetNode, AssetNodeDto>().ReverseMap();
            CreateMap<AssetSignals, AssetSignalDto>().ReverseMap();
        }
    }
}