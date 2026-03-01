using LoginAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoginAPI.Data;

/// <summary>
/// Seeds an optional demo user at application startup.
/// </summary>
public static class DemoUserSeeder
{
    private const string DefaultRoleName = "User";
    private const string AdminRoleName = "Admin";

    /// <summary>
    /// Seeds a demo user when required configuration values are present.
    /// </summary>
    /// <param name="services">Root service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DemoUserSeeder");
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = GetValue(configuration, "DEMO_USER_EMAIL", "DemoUser:Email");
        var password = GetValue(configuration, "DEMO_USER_PASSWORD", "DemoUser:Password");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Skipping demo user seeding because demo credentials are not configured.");
            return;
        }

        var firstName = GetValue(configuration, "DEMO_USER_FIRST_NAME", "DemoUser:FirstName") ?? "Demo";
        var lastName = GetValue(configuration, "DEMO_USER_LAST_NAME", "DemoUser:LastName") ?? "User";
        email = email.Trim().ToLowerInvariant();

        var now = DateTime.UtcNow;

        var defaultRole = await EnsureRoleAsync(
            dbContext,
            DefaultRoleName,
            "Default role for registered users",
            now,
            cancellationToken);

        var adminRole = await EnsureRoleAsync(
            dbContext,
            AdminRoleName,
            "Administrative role for privileged users",
            now,
            cancellationToken);

        var existingUser = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser != null)
        {
            var userRolesUpdated = EnsureUserHasRole(existingUser, defaultRole, now)
                | EnsureUserHasRole(existingUser, adminRole, now);

            if (userRolesUpdated)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Demo user updated with required roles: {Email}", email);
            }
            else
            {
                logger.LogInformation("Demo user already exists with required roles: {Email}", email);
            }

            return;
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            UserRoles = new List<UserRole>
            {
                new()
                {
                    Role = defaultRole,
                    AssignedAt = now
                },
                new()
                {
                    Role = adminRole,
                    AssignedAt = now
                }
            }
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo user seeded successfully: {Email}", email);
    }

    /// <summary>
    /// Ensures a role with the provided name exists.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="roleName">Role name to find or create.</param>
    /// <param name="description">Role description when creating a new role.</param>
    /// <param name="now">Current UTC timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly-created role.</returns>
    private static async Task<Role> EnsureRoleAsync(
        AppDbContext dbContext,
        string roleName,
        string description,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

        if (role != null)
        {
            return role;
        }

        role = new Role
        {
            Name = roleName,
            Description = description,
            CreatedAt = now
        };

        dbContext.Roles.Add(role);
        return role;
    }

    /// <summary>
    /// Ensures the given user has the specified role assignment.
    /// </summary>
    /// <param name="user">User to update.</param>
    /// <param name="role">Role to assign when missing.</param>
    /// <param name="assignedAt">Assignment timestamp.</param>
    /// <returns><c>true</c> when a new assignment was added; otherwise, <c>false</c>.</returns>
    private static bool EnsureUserHasRole(User user, Role role, DateTime assignedAt)
    {
        var hasRole = user.UserRoles.Any(ur =>
            ur.Role != null
            && string.Equals(ur.Role.Name, role.Name, StringComparison.OrdinalIgnoreCase));

        if (hasRole)
        {
            return false;
        }

        user.UserRoles.Add(new UserRole
        {
            User = user,
            Role = role,
            AssignedAt = assignedAt
        });

        return true;
    }

    /// <summary>
    /// Returns the first non-empty configuration value for the provided keys.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="keys">Candidate configuration keys.</param>
    /// <returns>The first non-empty value found; otherwise <see langword="null"/>.</returns>
    private static string? GetValue(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
