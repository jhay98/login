using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using LoginAPI.Data.Entities;
using LoginAPI.Interfaces;
using LoginAPI.Models;
using LoginAPI.Models.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LoginAPI.Services;

/// <summary>
/// Implements authentication and account workflows.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository used for user persistence and lookup.</param>
    /// <param name="jwtSettings">JWT configuration options.</param>
    /// <param name="logger">Logger for audit and diagnostics.</param>
    /// <param name="mapper">Mapper for entity-to-DTO conversion.</param>
    public AuthService(
        IUserRepository userRepository, 
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _mapper = mapper;
    }
    
    /// <inheritdoc />
    public async Task<UserDto> RegisterAsync(RegisterRequestDto request)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            throw new InvalidOperationException("Email is already registered");
        }
        
        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        
        // Create user entity
        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        // Save to database
        var createdUser = await _userRepository.CreateAsync(user);
        
        _logger.LogInformation("User registered successfully: {Email}", createdUser.Email);
        
        return _mapper.Map<UserDto>(createdUser);
    }
    
    /// <inheritdoc />
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        // Get user by email
        var user = await _userRepository.GetByEmailAsync(request.Email);
        
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // Verify password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        _logger.LogInformation("User logged in successfully: {Email}", user.Email);
        
        return new LoginResponseDto
        {
            Token = token,
            User = _mapper.Map<UserDto>(user)
        };
    }
    
    /// <inheritdoc />
    public async Task<UserDto> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            throw new KeyNotFoundException("User not found");
        }
        
        return _mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return _mapper.Map<List<UserDto>>(users);
    }
    
    /// <summary>
    /// Builds a signed JWT access token for the specified user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>A signed JWT token string.</returns>
    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId", user.Id.ToString())
        };

        foreach (var roleName in user.UserRoles.Select(ur => ur.Role.Name).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }
        
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
}