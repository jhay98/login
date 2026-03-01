using ActivityAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActivityAPI.Data;

/// <summary>
/// Database context for activity history records.
/// </summary>
public class ActivityDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityDbContext"/> class.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public ActivityDbContext(DbContextOptions<ActivityDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets activity history records.
    /// </summary>
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(120).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(80);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.Metadata).HasMaxLength(2048);
            entity.HasIndex(e => e.OccurredAtUtc);
        });
    }
}
