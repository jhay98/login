using LoginAPI.Models.DTOs;

namespace LoginAPI.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto> GetUserByIdAsync(int userId);
    Task<List<UserDto>> GetAllUsersAsync();
}
