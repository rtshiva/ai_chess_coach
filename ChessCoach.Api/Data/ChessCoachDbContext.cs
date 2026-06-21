using ChessCoach.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChessCoach.Api.Data;

public class ChessCoachDbContext : DbContext
{
    public ChessCoachDbContext(DbContextOptions<ChessCoachDbContext> options) : base(options)
    {
    }

    public DbSet<MoveAnalysisEvent> MoveAnalysisEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MoveAnalysisEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            
            // EventId BIGSERIAL equivalent for SQLite is INTEGER PRIMARY KEY AUTOINCREMENT
            // EF Core handles this conventionally for integer primary keys.

            // CONSTRAINT unique_ply_per_game UNIQUE (GameId, PlySequenceId)
            entity.HasIndex(e => new { e.GameId, e.PlySequenceId })
                  .IsUnique();

            // CREATE INDEX idx_user_analytics_lookup ON MoveAnalysisEvents (UserId, PrimaryCategory, SubCategory);
            entity.HasIndex(e => new { e.UserId, e.PrimaryCategory, e.SubCategory })
                  .HasDatabaseName("idx_user_analytics_lookup");

            entity.Property(e => e.Timestamp)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
