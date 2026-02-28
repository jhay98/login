using LoginAPI.Data.Entities;
using LoginAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LoginAPI.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private const string DefaultRoleName = "User";
    
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }
    
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id)
            .ToListAsync();
    }
    
    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == DefaultRoleName);
        if (defaultRole == null)
        {
            defaultRole = new Role
            {
                Name = DefaultRoleName,
                Description = "Default role for registered users",
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(defaultRole);
        }

        user.UserRoles.Add(new UserRole
        {
            User = user,
            Role = defaultRole,
            AssignedAt = DateTime.UtcNow
        });
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }
}