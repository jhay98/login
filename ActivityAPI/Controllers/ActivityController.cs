using ActivityAPI.Data;
using ActivityAPI.Data.Entities;
using ActivityAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityAPI.Controllers;

/// <summary>
/// Exposes activity history endpoints for trusted internal callers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "InternalApi")]
public class ActivityController : ControllerBase
{
    private readonly ActivityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityController"/> class.
    /// </summary>
    /// <param name="dbContext">Activity database context.</param>
    public ActivityController(ActivityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates an activity event.
    /// </summary>
    /// <param name="request">Activity payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created activity record.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ActivityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivityDto>> CreateActivity([FromBody] CreateActivityRequestDto request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return BadRequest(new ErrorResponseDto { Message = "userId must be greater than zero" });
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest(new ErrorResponseDto { Message = "eventType is required" });
        }

        var entity = new ActivityEvent
        {
            UserId = request.UserId,
            EventType = request.EventType.Trim(),
            IpAddress = Normalize(request.IpAddress),
            UserAgent = Normalize(request.UserAgent),
            Metadata = Normalize(request.Metadata),
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.ActivityEvents.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = Map(entity);
        return CreatedAtAction(nameof(GetRecentActivity), new { count = 1 }, response);
    }

    /// <summary>
    /// Gets most recent activity events.
    /// </summary>
    /// <param name="count">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent activity records ordered from newest to oldest.</returns>
    [HttpGet("{count:int}")]
    [ProducesResponseType(typeof(List<ActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ActivityDto>>> GetRecentActivity([FromRoute] int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return BadRequest(new ErrorResponseDto { Message = "count must be greater than zero" });
        }

        var safeCount = Math.Min(count, 200);

        var events = await _dbContext.ActivityEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(safeCount)
            .Select(e => new ActivityDto
            {
                Id = e.Id,
                UserId = e.UserId,
                EventType = e.EventType,
                IpAddress = e.IpAddress,
                UserAgent = e.UserAgent,
                Metadata = e.Metadata,
                OccurredAtUtc = e.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static ActivityDto Map(ActivityEvent entity)
    {
        return new ActivityDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            EventType = entity.EventType,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            Metadata = entity.Metadata,
            OccurredAtUtc = entity.OccurredAtUtc
        };
    }
}
