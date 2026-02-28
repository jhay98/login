using AutoMapper;
using LoginAPI.Data.Entities;
using LoginAPI.Models.DTOs;

namespace LoginAPI.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
