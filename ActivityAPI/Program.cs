using ActivityAPI.Authentication;
using ActivityAPI.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var activityConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=activity.db";

builder.Services.AddDbContext<ActivityDbContext>(options =>
{
    if (activityConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(activityConnectionString);
        return;
    }

    options.UseSqlite(activityConnectionString);
});

builder.Services.AddAuthentication("InternalApiKey")
    .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
        "InternalApiKey",
        _ =>
        {
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InternalApi", policy =>
    {
        policy.AddAuthenticationSchemes("InternalApiKey");
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("--migrate-only"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
    ApplyDatabaseSchema(dbContext);
    return;
}

if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
    ApplyDatabaseSchema(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

static void ApplyDatabaseSchema(ActivityDbContext dbContext)
{
    if (dbContext.Database.GetMigrations().Any())
    {
        dbContext.Database.Migrate();
        return;
    }

    dbContext.Database.EnsureCreated();
}

public partial class Program;
