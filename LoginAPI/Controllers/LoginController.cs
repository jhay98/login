using LoginAPI.Interfaces;
using LoginAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoginAPI.Controllers;

/// <summary>
/// Exposes authentication and user profile endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize(Policy = "InternalApi")]
public class LoginController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginController> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginController"/> class.
    /// </summary>
    /// <param name="authService">Authentication service.</param>
    /// <param name="logger">Logger instance.</param>
    public LoginController(IAuthService authService, ILogger<LoginController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">Registration request payload.</param>
    /// <returns>The created user.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(GetMeByUserId), new { userId = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed");
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }
    
    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login request payload.</param>
    /// <returns>Authenticated user profile and role names.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequestDto request)
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
    /// Gets a user profile by identifier for trusted internal callers.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The requested user profile.</returns>
    [HttpGet("me/{userId:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMeByUserId([FromRoute] int userId)
    {
        try
        {
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
    /// Gets all users for trusted internal callers.
    /// </summary>
    /// <returns>A list of users.</returns>
    [HttpGet("users/internal")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetAllUsersInternal()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }
}