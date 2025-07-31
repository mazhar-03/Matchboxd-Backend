namespace Matchboxd.API.Models;

public class Match
{
    public int Id { get; set; }
    public int? ExternalId { get; set; }

    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public DateTime MatchDate { get; set; }
    public string Status { get; set; } = "scheduled"; // scheduled, live, finished
    public int? ScoreHome { get; set; }
    public int? ScoreAway { get; set; }
    public string? Description { get; set; }

    public ICollection<Rating> Ratings { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}