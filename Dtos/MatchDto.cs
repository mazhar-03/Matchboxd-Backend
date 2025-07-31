namespace Matchboxd.API.Dtos;

public class MatchDto
{
    public int Id { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public DateTime MatchDate { get; set; }
    public string Status { get; set; }
    public int? ScoreHome { get; set; }
    public int? ScoreAway { get; set; }
    public string? Description { get; set; }
    public int WatchCount { get; set; }

    public List<RatingDto> Ratings { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
}