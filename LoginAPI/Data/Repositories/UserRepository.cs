using LoginAPI.Data.Entities;
using LoginAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LoginAPI.Data.Repositories;

/// <summary>
/// Entity Framework implementation of <see cref="IUserRepository"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private const string DefaultRoleName = "User";
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    
    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }
    
    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id)
            .ToListAsync();
    }
    
    /// <inheritdoc />
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
    
    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }
}