using System.Security.Claims;
using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/users/me/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUserReviews()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User not found in token");

        int userId = int.Parse(userIdClaim.Value);

        var matches = await _context.Matches
            .Include(m => m.Comments)
            .Include(m => m.Ratings)
            .Where(m => m.Comments.Any(c => c.UserId == userId) || m.Ratings.Any(r => r.UserId == userId))
            .ToListAsync(); 
        var result = new List<ReviewDto>();

        foreach (var match in matches)
        {
            var userComment = match.Comments.FirstOrDefault(c => c.UserId == userId);
            var userRating = match.Ratings.FirstOrDefault(r => r.UserId == userId);

            result.Add(new ReviewDto
            {
                MatchId = match.Id,
                HomeTeam = match.HomeTeam,
                AwayTeam = match.AwayTeam,
                Score = userRating?.Score,
                Comment = userComment?.Content,
                ReviewedAt = userComment?.CreatedAt ?? userRating?.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPost("remove")]
    public async Task<IActionResult> RemoveReview([FromBody] RemoveReviewDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var rating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MatchId == dto.MatchId);

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.UserId == userId && c.MatchId == dto.MatchId);

        if (rating == null && comment == null)
            return NotFound("No rating or comment found for this match.");

        if (rating != null)
            _context.Ratings.Remove(rating);

        if (comment != null)
            _context.Comments.Remove(comment);

        await _context.SaveChangesAsync();

        return Ok("Rating and/or comment removed successfully.");
    }

}