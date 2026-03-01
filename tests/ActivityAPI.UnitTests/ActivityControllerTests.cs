using ActivityAPI.Controllers;
using ActivityAPI.Data;
using ActivityAPI.Data.Entities;
using ActivityAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityAPI.UnitTests;

public class ActivityControllerTests
{
    [Fact]
    public async Task CreateActivity_WhenUserIdIsInvalid_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var controller = new ActivityController(dbContext);

        var result = await controller.CreateActivity(new CreateActivityRequestDto
        {
            UserId = 0,
            EventType = "login_success"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<ErrorResponseDto>(badRequest.Value);

        Assert.Equal("userId must be greater than zero", payload.Message);
        Assert.Empty(dbContext.ActivityEvents);
    }

    [Fact]
    public async Task CreateActivity_WhenEventTypeIsBlank_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var controller = new ActivityController(dbContext);

        var result = await controller.CreateActivity(new CreateActivityRequestDto
        {
            UserId = 7,
            EventType = "   "
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<ErrorResponseDto>(badRequest.Value);

        Assert.Equal("eventType is required", payload.Message);
        Assert.Empty(dbContext.ActivityEvents);
    }

    [Fact]
    public async Task CreateActivity_WhenPayloadIsValid_TrimsFieldsAndPersists()
    {
        await using var dbContext = CreateDbContext();
        var controller = new ActivityController(dbContext);

        var beforeCreate = DateTimeOffset.UtcNow;

        var result = await controller.CreateActivity(new CreateActivityRequestDto
        {
            UserId = 9,
            EventType = "  login_success  ",
            IpAddress = " 127.0.0.1 ",
            UserAgent = " test-agent ",
            Metadata = " {\"source\":\"unit\"} "
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payload = Assert.IsType<ActivityDto>(created.Value);

        Assert.Equal(nameof(ActivityController.GetRecentActivity), created.ActionName);
        Assert.Equal("login_success", payload.EventType);
        Assert.Equal("127.0.0.1", payload.IpAddress);
        Assert.Equal("test-agent", payload.UserAgent);
        Assert.Equal("{\"source\":\"unit\"}", payload.Metadata);
        Assert.True(payload.OccurredAtUtc >= beforeCreate);

        var saved = await dbContext.ActivityEvents.SingleAsync();
        Assert.Equal(9, saved.UserId);
        Assert.Equal("login_success", saved.EventType);
        Assert.Equal("127.0.0.1", saved.IpAddress);
        Assert.Equal("test-agent", saved.UserAgent);
        Assert.Equal("{\"source\":\"unit\"}", saved.Metadata);
    }

    [Fact]
    public async Task GetRecentActivity_WhenCountIsInvalid_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var controller = new ActivityController(dbContext);

        var result = await controller.GetRecentActivity(0, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<ErrorResponseDto>(badRequest.Value);

        Assert.Equal("count must be greater than zero", payload.Message);
    }

    [Fact]
    public async Task GetRecentActivity_WhenCountExceedsLimit_ReturnsMostRecentTwoHundred()
    {
        await using var dbContext = CreateDbContext();

        var seedTime = DateTimeOffset.UtcNow.AddHours(-1);
        for (var i = 0; i < 210; i++)
        {
            dbContext.ActivityEvents.Add(new ActivityEvent
            {
                UserId = i + 1,
                EventType = $"event-{i}",
                OccurredAtUtc = seedTime.AddSeconds(i)
            });
        }

        await dbContext.SaveChangesAsync();

        var controller = new ActivityController(dbContext);

        var result = await controller.GetRecentActivity(500, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<ActivityDto>>(ok.Value);

        Assert.Equal(200, payload.Count);
        Assert.Equal("event-209", payload[0].EventType);
        Assert.Equal("event-10", payload[^1].EventType);
    }

    private static ActivityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ActivityDbContext>()
            .UseInMemoryDatabase($"activity-unit-tests-{Guid.NewGuid():N}")
            .Options;

        return new ActivityDbContext(options);
    }
}
