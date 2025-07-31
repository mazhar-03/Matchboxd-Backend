using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Matchboxd.API.Models;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/users/me/favorites")]
[Authorize]  
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUserFavorites()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User not found in token");

        int userId = int.Parse(userIdClaim.Value);

        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Match)
            .Select(f => new FavoriteMatchDto
            {
                MatchId = f.MatchId,
                HomeTeam = f.Match.HomeTeam,
                AwayTeam = f.Match.AwayTeam,
                MatchDate = f.Match.MatchDate,
                Status = f.Match.Status,
            })
            .ToListAsync();

        return Ok(favorites);
    }
    
    [HttpGet("{matchId}")]
    public async Task<IActionResult> IsFavorite(int matchId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User not found in token");

        int userId = int.Parse(userIdClaim.Value);

        var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.MatchId == matchId);

        return Ok(new { hasFavorited = exists });
    }
    
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveFromFavorites([FromBody] RemoveFavoriteDto dto)
    {
        var userId = await FindUserService.GetCurrentUserIdAsync(User, _context);

        var favoriteItem = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MatchId == dto.MatchId);

        if (favoriteItem == null)
            return NotFound("Match is not in your favorites.");

        _context.Favorites.Remove(favoriteItem);
        await _context.SaveChangesAsync();

        return Ok("Match removed from favorites.");
    }
    
    [HttpPost("toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite([FromBody] ToggleFavoriteDto dto)
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var match = await _context.Matches.FindAsync(dto.MatchId);
        if (match == null)
            return NotFound("Match not found");

        var fav = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MatchId == dto.MatchId);

        if (fav != null)
        {
            _context.Favorites.Remove(fav);
            await _context.SaveChangesAsync();
            return Ok("Removed from favorites");
        }

        _context.Favorites.Add(new Favorite
        {
            MatchId = dto.MatchId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok("Added to favorites");
    }
}