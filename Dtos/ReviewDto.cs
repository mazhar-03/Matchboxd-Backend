namespace Matchboxd.API.Dtos;

public class ReviewDto
{
    public int MatchId { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public double? Score { get; set; }
    public string? Comment { get; set; }
    public DateTime? ReviewedAt { get; set; } // <-- bunu nullable yap!
}
