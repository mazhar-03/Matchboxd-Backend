using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/users")]
[Authorize]  
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }
    
    
    

    [HttpGet("me/diary")]
    public async Task<IActionResult> GetUserDiary()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User not found in token");

        int userId = int.Parse(userIdClaim.Value);

        var watchedMatches = await _context.WatchedMatches
            .Where(w => w.UserId == userId)
            .Include(w => w.Match)
            .ToListAsync();

        var matchIds = watchedMatches.Select(w => w.MatchId).ToList();

        var ratings = await _context.Ratings
            .Where(r => matchIds.Contains(r.MatchId) && r.UserId == userId)
            .ToListAsync();

        var comments = await _context.Comments
            .Where(c => matchIds.Contains(c.MatchId) && c.UserId == userId)
            .ToListAsync();

        var favorites = await _context.Favorites
            .Where(f => matchIds.Contains(f.MatchId) && f.UserId == userId)
            .ToListAsync();

        var diaryEntries = watchedMatches
            .Select(w => {
                var rating = ratings.FirstOrDefault(r => r.MatchId == w.MatchId);
                var comment = comments.FirstOrDefault(c => c.MatchId == w.MatchId);
                var isFavorite = favorites.Any(f => f.MatchId == w.MatchId);

                return new DiaryEntryDto
                {
                    MatchId = w.MatchId,
                    HomeTeam = w.Match.HomeTeam,
                    AwayTeam = w.Match.AwayTeam,
                    MatchDate = w.Match.MatchDate,
                    Score = rating?.Score,
                    Comment = comment?.Content,
                    Favorite = isFavorite,
                    WatchedAt = w.WatchedAt
                };
            })
            .OrderByDescending(d => d.WatchedAt)
            .ToList();

        return Ok(diaryEntries);
    }

    [HttpGet("me/matches/{matchId}/watched")]
    public async Task<IActionResult> HasWatchedMatch(int matchId)
    {
        var userId = await FindUserService.GetCurrentUserIdAsync(User, _context);
        var hasWatched = await _context.WatchedMatches
            .AnyAsync(w => w.UserId == userId && w.MatchId == matchId);

        return Ok(new { hasWatched });
    }
}
