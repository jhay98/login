using LoginAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoginAPI.Data;

public static class DemoUserSeeder
{
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

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo user seeded successfully: {Email}", email);
    }

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
