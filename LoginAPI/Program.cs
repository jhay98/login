using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using LoginAPI.Authentication;
using LoginAPI.Data;
using LoginAPI.Data.Repositories;
using LoginAPI.Interfaces;
using LoginAPI.Middleware;
using LoginAPI.Services;
using LoginAPI.Validators;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("--migrate-only"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    return;
}

if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

await DemoUserSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.

// Global Exception Middleware (should be first)
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;