namespace Matchboxd.API.Models;

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MatchId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; }
    public Match Match { get; set; }
}