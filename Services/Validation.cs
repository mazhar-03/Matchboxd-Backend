using System.Text.RegularExpressions;

namespace Matchboxd.API.Services;

public static class Validation
{
    public static string? ValidateEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "Email is required.";

        try 
        {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            if (mailAddress.Address != email)
                return "Please enter a valid email address (e.g., user@example.com).";
        }
        catch
        {
            return "Please enter a valid email address (e.g., user@example.com).";
        }

        return null;
    }

    public static string? ValidateUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return "Username is required.";

        if (username.Length < 3 || username.Length > 20)
            return "Username must be between 3-20 characters.";

        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            return "Username can only contain letters, numbers, and underscores.";

        return null;
    }

    public static string? ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Password is required.";

        if (password.Length < 8)
            return "Password must be at least 8 characters.";

        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";

        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter.";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one number.";

        return null;
    }
}