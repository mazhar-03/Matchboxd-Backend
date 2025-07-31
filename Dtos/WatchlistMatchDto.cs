namespace Matchboxd.API.Dtos;

public class WatchlistMatchDto
{
    public int MatchId { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public DateTime MatchDate { get; set; }
    public string Status { get; set; }
}