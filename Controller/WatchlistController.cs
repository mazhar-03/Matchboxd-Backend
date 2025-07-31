using System.Security.Claims;
using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Matchboxd.API.Models;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/watchlist")]
public class WatchlistController : ControllerBase
{
    private readonly AppDbContext _context;

    public WatchlistController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetWatchlist()
    {
        var userId = await FindUserService.GetCurrentUserIdAsync(User, _context); 

        var watchlistMatches = await _context.WatchlistItems
            .Where(w => w.UserId == userId)
            .Include(w => w.Match)
            .ThenInclude(m => m.Ratings)
            .Include(w => w.Match)
            .ThenInclude(m => m.Comments)
            .Select(w => new MatchSummaryDto
            {
                Id = w.Match.Id,
                HomeTeam = w.Match.HomeTeam,
                AwayTeam = w.Match.AwayTeam,
                MatchDate = w.Match.MatchDate,
                Status = w.Match.Status,
                ScoreHome = w.Match.ScoreHome,
                ScoreAway = w.Match.ScoreAway,
                Description = w.Match.Description,
                AverageRating = w.Match.Ratings.Any() ? w.Match.Ratings.Average(r => r.Score) : 0,
                TotalComments = w.Match.Comments.Count,
                WatchCount = _context.WatchedMatches.Count(wm => wm.MatchId == w.Match.Id) // 👈 Dinamik hesaplama
            })
            .ToListAsync();

        return Ok(watchlistMatches);
    }


    [HttpPost("add")]
    public async Task<IActionResult> AddToWatchlist([FromBody] AddToWatchlistDto dto)
    {
        try
        {
            var userId = await FindUserService.GetCurrentUserIdAsync(User, _context);

            // Aynı maç zaten watchlist'te mi?
            var alreadyExists = await _context.WatchlistItems
                .AnyAsync(w => w.UserId == userId && w.MatchId == dto.MatchId);

            if (alreadyExists)
                return BadRequest("This match is already in your watchlist.");

            // Match var mı kontrol et
            var matchExists = await _context.Matches.AnyAsync(m => m.Id == dto.MatchId);
            if (!matchExists)
                return NotFound("Match not found.");

            var item = new WatchlistItem
            {
                UserId = userId,
                MatchId = dto.MatchId
            };

            _context.WatchlistItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok("Match added to watchlist.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveFromWatchlist([FromBody] AddToWatchlistDto dto)
    {
        try
        {
            var userId = await FindUserService.GetCurrentUserIdAsync(User, _context); // Your JWT or auth logic

            var item = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MatchId == dto.MatchId);

            if (item == null)
                return NotFound("Match is not in your watchlist.");

            _context.WatchlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok("Match removed from watchlist.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}