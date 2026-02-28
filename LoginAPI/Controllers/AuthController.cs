using System.Security.Claims;
using LoginAPI.Interfaces;
using LoginAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoginAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(GetMe), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed");
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }
    
    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed");
            return Unauthorized(new ErrorResponseDto { Message = ex.Message });
        }
    }
    
    /// <summary>
    /// Get current user details (requires authentication)
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        try
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ErrorResponseDto { Message = "Invalid token" });
            }
            
            var user = await _authService.GetUserByIdAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(new ErrorResponseDto { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get all users (admin role required)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }
}