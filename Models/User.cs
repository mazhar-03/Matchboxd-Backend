namespace Matchboxd.API.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool EmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }

    public ICollection<Rating> Ratings { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Favorite> Favorites { get; set; }
}