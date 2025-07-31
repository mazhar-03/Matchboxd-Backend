namespace Matchboxd.API.Dtos;

public class FavoriteMatchDto
{
    public int MatchId { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public DateTime MatchDate { get; set; } // renamed from StartTime
    public string Status { get; set; }
}
