using LoginAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoginAPI.Data;

/// <summary>
/// Seeds an optional demo user at application startup.
/// </summary>
public static class DemoUserSeeder
{
    private const string DefaultRoleName = "User";

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

        var userExists = await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (userExists)
        {
            logger.LogInformation("Demo user already exists: {Email}", email);
            return;
        }

        var now = DateTime.UtcNow;

        var defaultRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == DefaultRoleName, cancellationToken);

        if (defaultRole == null)
        {
            defaultRole = new Role
            {
                Name = DefaultRoleName,
                Description = "Default role for registered users",
                CreatedAt = now
            };

            dbContext.Roles.Add(defaultRole);
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
                }
            }
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo user seeded successfully: {Email}", email);
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
