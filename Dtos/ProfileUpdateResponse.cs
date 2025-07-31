namespace Matchboxd.API.Dtos;

public class ProfileUpdateResponse
{
    public string Message { get; set; }
    public string Token { get; set; }
    public string? AvatarUrl { get; set; } 
    public string? Username { get; set; } 
}