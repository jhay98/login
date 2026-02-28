using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using LoginAPI.Data.Entities;
using LoginAPI.Interfaces;
using LoginAPI.Mappings;
using LoginAPI.Models;
using LoginAPI.Models.DTOs;
using LoginAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LoginAPI.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        var repository = new FakeUserRepository { EmailExistsResult = true };
        var service = CreateService(repository);

        var request = new RegisterRequestDto
        {
            Email = "existing@example.com",
            Password = "Passw0rd!",
            FirstName = "Jane",
            LastName = "Doe"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WhenRequestIsValid_CreatesUserAndReturnsMappedDto()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var request = new RegisterRequestDto
        {
            Email = "TestUser@Example.com",
            Password = "Passw0rd!",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = await service.RegisterAsync(request);

        Assert.NotNull(repository.CreatedUser);
        Assert.Equal("testuser@example.com", repository.CreatedUser!.Email);
        Assert.NotEqual(request.Password, repository.CreatedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(request.Password, repository.CreatedUser.PasswordHash));

        Assert.Equal(repository.CreatedUser.Id, result.Id);
        Assert.Equal("testuser@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ThrowsUnauthorizedAccessException()
    {
        var repository = new FakeUserRepository { UserByEmail = null };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = "missing@example.com",
            Password = "Passw0rd!"
        }));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsUnauthorizedAccessException()
    {
        var repository = new FakeUserRepository
        {
            UserByEmail = new User
            {
                Id = 11,
                Email = "user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DifferentPassw0rd!"),
                FirstName = "Test",
                LastName = "User"
            }
        };

        var service = CreateService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "Passw0rd!"
        }));
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsTokenAndUser()
    {
        var user = new User
        {
            Id = 7,
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd!"),
            FirstName = "Test",
            LastName = "User",
            UserRoles =
            [
                new UserRole
                {
                    Role = new Role { Name = "Admin" }
                }
            ]
        };

        var repository = new FakeUserRepository { UserByEmail = user };
        var service = CreateService(repository);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "user@example.com",
            Password = "Passw0rd!"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Email, result.User.Email);

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Contains(parsedToken.Claims, c => c.Type == "userId" && c.Value == "7");
        Assert.Contains(parsedToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserIsMissing_ThrowsKeyNotFoundException()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserByIdAsync(404));
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsMappedUserDtos()
    {
        var repository = new FakeUserRepository
        {
            AllUsers =
            [
                new User { Id = 1, Email = "one@example.com", FirstName = "One", LastName = "User" },
                new User { Id = 2, Email = "two@example.com", FirstName = "Two", LastName = "User" }
            ]
        };

        var service = CreateService(repository);

        var result = await service.GetAllUsersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("one@example.com", result[0].Email);
        Assert.Equal("two@example.com", result[1].Email);
    }

    private static AuthService CreateService(FakeUserRepository repository)
    {
        var jwtOptions = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-with-at-least-32-characters",
            Issuer = "LoginAPI.Tests",
            Audience = "LoginAPI.Tests.Client",
            ExpirationMinutes = 60
        });

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<UserMappingProfile>()).CreateMapper();

        return new AuthService(repository, jwtOptions, NullLogger<AuthService>.Instance, mapper);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? UserByEmail { get; set; }
        public Dictionary<int, User> UsersById { get; } = new();
        public List<User> AllUsers { get; set; } = [];
        public bool EmailExistsResult { get; set; }
        public User? CreatedUser { get; private set; }

        public Task<User?> GetByEmailAsync(string email)
        {
            return Task.FromResult(UserByEmail);
        }

        public Task<User?> GetByIdAsync(int id)
        {
            UsersById.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task<List<User>> GetAllAsync()
        {
            return Task.FromResult(AllUsers);
        }

        public Task<User> CreateAsync(User user)
        {
            user.Id = 100;
            CreatedUser = user;
            return Task.FromResult(user);
        }

        public Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(EmailExistsResult);
        }
    }
}
