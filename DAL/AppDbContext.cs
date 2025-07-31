using Matchboxd.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Matchboxd.API.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<WatchedMatch> WatchedMatches => Set<WatchedMatch>();
    public DbSet<WatchlistItem> WatchlistItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WatchlistItem>()
            .HasIndex(w => new { w.UserId, w.MatchId })
            .IsUnique(); // prevent duplicates
    }
}