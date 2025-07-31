namespace Matchboxd.API.Dtos;

public class UpdateUserProfileDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public IFormFile? ProfileImage { get; set; }
    
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}