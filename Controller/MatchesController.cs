using System.Security.Claims;
using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Matchboxd.API.Models;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MatchesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMatches()
    {
        try
        {
            var matches = await _context.Matches
                .Include(m => m.Ratings)
                .Include(m => m.Comments)
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();

            var matchIds = matches.Select(m => m.Id).ToList();

            var watchCounts = await _context.WatchedMatches
                .Where(w => matchIds.Contains(w.MatchId))
                .GroupBy(w => w.MatchId)
                .Select(g => new { MatchId = g.Key, Count = g.Select(w => w.UserId).Distinct().Count() })
                .ToDictionaryAsync(g => g.MatchId, g => g.Count);

            var matchSummaries = matches.Select(m => new MatchSummaryDto
            {
                Id = m.Id,
                HomeTeam = m.HomeTeam,
                AwayTeam = m.AwayTeam,
                MatchDate = m.MatchDate,
                Status = m.Status,
                ScoreHome = m.ScoreHome,
                ScoreAway = m.ScoreAway,
                AverageRating = m.Ratings.Any() ? m.Ratings.Average(r => r.Score) : 0,
                TotalComments = m.Comments.Count,
                WatchCount = watchCounts.ContainsKey(m.Id) ? watchCounts[m.Id] : 0
            }).ToList();

            return Ok(matchSummaries);
        }
        catch (Exception e)
        {
            return BadRequest($"Error while getting matches: {e.Message}");
        }
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetMatchById(int id)
    {
        try
        {
            var match = await _context.Matches
                .Include(m => m.Ratings).ThenInclude(r => r.User)
                .Include(m => m.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
                return NotFound();

            var watchCount = await _context.WatchedMatches
                .Where(w => w.MatchId == id)
                .Select(w => w.UserId)
                .Distinct()
                .CountAsync();

            var dto = new MatchDto
            {
                Id = match.Id,
                HomeTeam = match.HomeTeam,
                AwayTeam = match.AwayTeam,
                MatchDate = match.MatchDate,
                Status = match.Status,
                ScoreHome = match.ScoreHome,
                ScoreAway = match.ScoreAway,
                Description = match.Description,
                WatchCount = watchCount,
                Ratings = match.Ratings.Select(r => new RatingDto
                {
                    Score = r.Score,
                    Username = r.User.Username,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                Comments = match.Comments.Select(c => new CommentDto
                {
                    Content = c.Content,
                    Username = c.User.Username,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception e)
        {
            return BadRequest("Error while getting match: " + e.Message);
        }
    }

    //Showing how many ppl watched that match
    [HttpGet("{matchId}/watched/count")]
    public async Task<IActionResult> GetWatchedCount(int matchId)
    {
        var count = await _context.WatchedMatches
            .CountAsync(w => w.MatchId == matchId);

        return Ok(new { watchedCount = count });
    }

    [HttpPost("{id}/rate-comment")]
    [Authorize]
    public async Task<IActionResult> RateAndCommentMatch(int id, [FromBody] CreateRatingCommentDto dto)
{
    try
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User not found in token");

        int userId = int.Parse(userIdClaim.Value);
        
        var match = await _context.Matches.FindAsync(id);
        if (match == null)
            return NotFound("Match not found");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        bool hasRating = dto.Score.HasValue;
        bool hasComment = !string.IsNullOrWhiteSpace(dto.Content);

        if (!hasRating && !hasComment)
            return BadRequest("Either a rating or comment must be provided.");

        if (hasRating)
        {
            if (dto.Score != null)
            {
                var rating = new Rating
                {
                    MatchId = id,
                    UserId = userId,
                    Score = dto.Score.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Ratings.Add(rating);
            }
        }

        if (hasComment)
        {
            if (match.Status != "FINISHED")
                return BadRequest("Cannot comment before match is finished");

            if (dto.Content != null)
            {
                var comment = new Comment
                {
                    MatchId = id,
                    UserId = userId,
                    Content = dto.Content,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Comments.Add(comment);
            }
        }

        // 🚨 Automatically add to WatchedMatch if not already exists
        bool alreadyWatched = await _context.WatchedMatches
            .AnyAsync(w => w.UserId == userId && w.MatchId == id);

        if (!alreadyWatched)
        {
            var watched = new WatchedMatch
            {
                UserId = userId,
                MatchId = id,
                WatchedAt = DateTime.UtcNow
            };
            _context.WatchedMatches.Add(watched);
        }

        await _context.SaveChangesAsync();
        return Ok("Rating and/or comment added. Match marked as watched.");
    }
    catch (Exception e)
    {
        return BadRequest("Error while adding comment and/or rating: " + e.Message);
    }
}
    [HttpPut("{matchId}/rate-comment")]
[Authorize]
public async Task<IActionResult> UpdateCommentAndRating(int matchId, [FromBody] CreateRatingCommentDto dto)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
        return Unauthorized("User not found in token");

    int userId = int.Parse(userIdClaim.Value);

    // Yorumu kontrol et
    var comment = await _context.Comments
        .FirstOrDefaultAsync(c => c.MatchId == matchId && c.UserId == userId);

    if (comment != null && !string.IsNullOrWhiteSpace(dto.Content))
    {
        comment.Content = dto.Content;
        comment.CreatedAt = DateTime.UtcNow;
    }
    else if (comment == null && !string.IsNullOrWhiteSpace(dto.Content))
    {
        // Yeni yorum ekle (isteğe bağlı)
        _context.Comments.Add(new Comment
        {
            UserId = userId,
            MatchId = matchId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        });
    }

    // Rating kontrol et
    var rating = await _context.Ratings
        .FirstOrDefaultAsync(r => r.MatchId == matchId && r.UserId == userId);

    if (rating != null && dto.Score.HasValue)
    {
        rating.Score = dto.Score.Value;
        rating.CreatedAt = DateTime.UtcNow;
    }
    else if (rating == null && dto.Score.HasValue)
    {
        // Yeni rating ekle (isteğe bağlı)
        _context.Ratings.Add(new Rating
        {
            UserId = userId,
            MatchId = matchId,
            Score = dto.Score.Value,
            CreatedAt = DateTime.UtcNow
        });
    }

    // Watched kaydı da ekle
    var watched = await _context.WatchedMatches
        .FirstOrDefaultAsync(w => w.UserId == userId && w.MatchId == matchId);
    if (watched == null)
    {
        _context.WatchedMatches.Add(new WatchedMatch
        {
            UserId = userId,
            MatchId = matchId,
            WatchedAt = DateTime.UtcNow
        });
    }

    await _context.SaveChangesAsync();
    return Ok("Updated");
}

    [HttpPost("{matchId}/watch")]
    public async Task<IActionResult> MarkAsWatched(int matchId)
    {
        var userId = await FindUserService.GetCurrentUserIdAsync(User, _context); 

        var alreadyWatched = await _context.WatchedMatches
            .AnyAsync(w => w.UserId == userId && w.MatchId == matchId);

        if (alreadyWatched)
            return BadRequest("You already marked this match as watched.");

        _context.WatchedMatches.Add(new WatchedMatch
        {
            UserId = userId,
            MatchId = matchId
        });

        await _context.SaveChangesAsync();
        return Ok("Match marked as watched.");
    }
    
    [HttpDelete("{matchId}/watch")]
    public async Task<IActionResult> UnmarkAsWatched(int matchId)
    {
        var userId = await FindUserService.GetCurrentUserIdAsync(User, _context);

        var watchedMatch = await _context.WatchedMatches
            .FirstOrDefaultAsync(w => w.UserId == userId && w.MatchId == matchId);

        if (watchedMatch == null)
            return NotFound("You have not marked this match as watched.");

        _context.WatchedMatches.Remove(watchedMatch);
        await _context.SaveChangesAsync();

        return Ok("Match unmarked as watched.");
    }


    [HttpPost("{id}/favorite")]
    [Authorize]
    public async Task<IActionResult> FavoriteMatch(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Invalid or missing user ID in token");

            var match = await _context.Matches.FindAsync(id);
            if (match == null)
                return NotFound("Match not found");

            var alreadyExists = await _context.Favorites
                .AnyAsync(f => f.MatchId == id && f.UserId == userId);

            if (alreadyExists)
                return BadRequest("Match is already in favorites");

            var fav = new Favorite
            {
                MatchId = id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Favorites.Add(fav);
            await _context.SaveChangesAsync();

            return Ok("Match added to favorites");
        }
        catch (Exception e)
        {
            return StatusCode(500, "Server error: " + e.Message);
        }
    }
    
    [HttpGet("{matchId}/comments")]
    public async Task<IActionResult> GetCommentsForMatch(int matchId)
    {
        var comments = await _context.Comments
            .Where(c => c.MatchId == matchId && !string.IsNullOrWhiteSpace(c.Content))
            .Include(c => c.User)  // Eğer yorum yapan kullanıcı bilgisi de gerekiyorsa
            .Select(c => new 
            {
                c.Id,
                c.Content,
                c.CreatedAt,
                UserName = c.User.Username
            })
            .ToListAsync();

        return Ok(comments);
    }

}