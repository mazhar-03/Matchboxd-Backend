namespace Matchboxd.API.Models;

public class WatchedMatch
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; }

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
}