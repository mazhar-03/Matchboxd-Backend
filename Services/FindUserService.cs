using System.Security.Claims;
using Matchboxd.API.DAL;
using Microsoft.EntityFrameworkCore;
using Matchboxd.API.Models;

namespace Matchboxd.API.Services;

public static class FindUserService
{
    public static async Task<int> GetCurrentUserIdAsync(
        ClaimsPrincipal user,
        AppDbContext context)
    {
        foreach (var claim in user.Claims)
        {
            Console.WriteLine($"Claim type: {claim.Type}, value: {claim.Value}");
        }

        // 1. Önce 'sub' veya 'NameIdentifier' claim'ine bak
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        // 2. Alternatif olarak kullanıcı adını dene
        var usernameClaim = user.FindFirst(ClaimTypes.Name)?.Value
                            ?? user.FindFirst("username")?.Value;

        if (string.IsNullOrEmpty(usernameClaim))
            throw new UnauthorizedAccessException("User identifier not found in claims.");

        var foundUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == usernameClaim);

        return foundUser?.Id ?? throw new UnauthorizedAccessException("User not found in database.");
    }
}