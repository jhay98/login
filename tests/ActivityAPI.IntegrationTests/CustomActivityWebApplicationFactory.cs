using ActivityAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityAPI.IntegrationTests;

public class CustomActivityWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvironmentValues = new();
    private readonly string _databaseName = $"activity-tests-{Guid.NewGuid():N}";

    public CustomActivityWebApplicationFactory()
    {
        SetEnvironmentVariable("InternalApi__Key", "integration-internal-key");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ActivityDbContext>));

            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            var dbContextOptionsConfigurationDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ActivityDbContext>));

            if (dbContextOptionsConfigurationDescriptor is not null)
            {
                services.Remove(dbContextOptionsConfigurationDescriptor);
            }

            services.AddDbContext<ActivityDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            foreach (var keyValuePair in _originalEnvironmentValues)
            {
                Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        _originalEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }
}
