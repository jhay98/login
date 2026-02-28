using LoginAPI.Data.Entities;

namespace LoginAPI.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}
