namespace Matchboxd.API.Dtos;

public class DiaryEntryDto
{
    public int MatchId { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public DateTime MatchDate { get; set; }
    public double? Score { get; set; }
    public string? Comment { get; set; }
    public bool Favorite { get; set; }
    public DateTime WatchedAt { get; set; }
    
    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
}
