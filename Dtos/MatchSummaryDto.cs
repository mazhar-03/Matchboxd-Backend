namespace Matchboxd.API.Dtos;

public class MatchSummaryDto
{
    public int Id { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public DateTime MatchDate { get; set; }
    public string Status { get; set; }
    public int? ScoreHome { get; set; }
    public int? ScoreAway { get; set; }
    public int WatchCount { get; set; }

    public string? Description { get; set; } // 🔧 Bunu ekle

    public double AverageRating { get; set; }
    public int TotalComments { get; set; }
}