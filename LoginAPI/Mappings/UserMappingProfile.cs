using AutoMapper;
using LoginAPI.Data.Entities;
using LoginAPI.Models.DTOs;
using System.Linq;

namespace LoginAPI.Mappings;

/// <summary>
/// AutoMapper profile for user-related mappings.
/// </summary>
public class UserMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserMappingProfile"/> class.
    /// </summary>
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(
                dest => dest.Roles,
                opt => opt.MapFrom(src => src.UserRoles
                    .Select(ur => ur.Role.Name)
                    .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()));
    }
}
