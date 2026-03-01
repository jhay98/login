using AutoMapper;
using LoginAPI.Data.Entities;
using LoginAPI.Interfaces;
using LoginAPI.Models.DTOs;

namespace LoginAPI.Services;

/// <summary>
/// Implements authentication and account workflows.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository used for user persistence and lookup.</param>
    /// <param name="logger">Logger for audit and diagnostics.</param>
    /// <param name="mapper">Mapper for entity-to-DTO conversion.</param>
    public AuthService(
        IUserRepository userRepository, 
        ILogger<AuthService> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
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
    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request)
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
        
        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return CreateLoginResult(user);
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
    /// Creates a login payload for a user containing profile information and roles.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>Login payload containing user profile and role names.</returns>
    private LoginResultDto CreateLoginResult(User user)
    {
        var roles = user.UserRoles
            .Select(ur => ur.Role?.Name)
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Select(roleName => roleName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LoginResultDto
        {
            User = _mapper.Map<UserDto>(user),
            Roles = roles
        };
    }
    
}