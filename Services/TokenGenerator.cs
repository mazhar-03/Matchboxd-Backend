namespace Matchboxd.API.Services;

public static class TokenGenerator
{
    public static string GenerateVerificationToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}