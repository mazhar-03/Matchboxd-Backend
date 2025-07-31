namespace Matchboxd.API.Dtos;

public class VerifyEmailDto
{
    public string Token { get; set; } = null!;
}

public class ResendVerificationDto
{
    public string Email { get; set; } = null!;
}