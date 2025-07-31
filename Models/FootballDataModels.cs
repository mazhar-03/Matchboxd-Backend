namespace Matchboxd.API.Models;

public class MatchesResponse
{
    public List<FootballMatch> Matches { get; set; }
}

public class FootballMatch
{
    public int Id { get; set; }
    public string Status { get; set; }
    public DateTime UtcDate { get; set; }
    public Team HomeTeam { get; set; }
    public Team AwayTeam { get; set; }
    public Competition Competition { get; set; }
    public string Stage { get; set; }
}

public class Team
{
    public string Name { get; set; }
}

public class Competition
{
    public string Name { get; set; }
}