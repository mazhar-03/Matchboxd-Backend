using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Matchboxd.API.Models;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly ITokenService _tokenService;
    private readonly ILogger<SettingsController> _logger;
    private readonly CloudinaryService _cloudinaryService;

    public SettingsController(AppDbContext context, ITokenService tokenService, EmailService emailService,
        ILogger<SettingsController> logger, CloudinaryService cloudinaryService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    [Authorize]
[HttpPut]
public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserProfileDto dto)
{
    _logger.LogInformation("Received update request");
    _logger.LogInformation($"Username: {dto.Username ?? "null"}");
    _logger.LogInformation($"Email: {dto.Email ?? "null"}");
    _logger.LogInformation($"ProfileImage: {(dto.ProfileImage != null ? "exists" : "null")}");
    
    // Start transaction
    await using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // 1. Get User
        var usernameClaim = User.FindFirstValue(ClaimTypes.Name) ?? 
                          User.FindFirstValue("username") ?? 
                          User.FindFirstValue("sub");
        
        _logger.LogInformation("Updating profile for user: {UsernameClaim}", usernameClaim);
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == usernameClaim);
        
        if (user == null)
        {
            _logger.LogWarning("User not found for claim: {UsernameClaim}", usernameClaim);
            return NotFound("User not found.");
        }

        // 2. Track Changes
        var changesDetected = false;
        var originalValues = new {
            Username = user.Username,
            Email = user.Email,
            ProfileImage = user.ProfileImageUrl
        };

        // 3. Update Username
        if (!string.IsNullOrWhiteSpace(dto.Username) && 
            !dto.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
        {
            if (await IsUsernameTaken(dto.Username, user.Username))
            {
                _logger.LogWarning("Username already taken: {NewUsername}", dto.Username);
                return BadRequest("Username is already taken.");
            }
            
            user.Username = dto.Username;
            changesDetected = true;
            _logger.LogInformation("Updating username from {Old} to {New}", 
                originalValues.Username, dto.Username);
        }

        // 4. Update Email
        if (!string.IsNullOrWhiteSpace(dto.Email) && 
            !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await IsEmailTaken(dto.Email, user.Email))
            {
                _logger.LogWarning("Email already taken: {NewEmail}", dto.Email);
                return BadRequest("Email is already taken.");
            }
            
            user.Email = dto.Email;
            user.EmailVerified = false;
            changesDetected = true;
            _logger.LogInformation("Updating email from {Old} to {New}", 
                originalValues.Email, dto.Email);
            
            await SendVerificationEmail(user);
        }

        // 5. Confirm password mismatch
        if (!string.IsNullOrWhiteSpace(dto.NewPassword) &&
            !string.IsNullOrWhiteSpace(dto.CurrentPassword) &&
            dto.NewPassword != dto.ConfirmNewPassword)
        {
            _logger.LogWarning("New password and confirmation do not match");
            return BadRequest(new { message = "New password and confirmation do not match" });
        }

// 5. Update Password
        if (!string.IsNullOrWhiteSpace(dto.CurrentPassword) && 
            !string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var passwordError = await UpdatePassword(user, dto.CurrentPassword, dto.NewPassword);
            if (passwordError != null)
            {
                _logger.LogWarning("Password update failed: {Error}", passwordError);
                return BadRequest(new { message = passwordError });
            }
            changesDetected = true;
        }


        // 6. Update Profile Image
        if (dto.ProfileImage != null)
        {
            try
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    var oldPublicId = GetPublicIdFromUrl(user.ProfileImageUrl);
                    await _cloudinaryService.DeleteImageAsync(oldPublicId);
                }

                // Upload new image
                var uploadResult = await _cloudinaryService.UploadImageAsync(
                    dto.ProfileImage, 
                    "matchboxd/profiles");
            
                user.ProfileImageUrl = uploadResult.SecureUrl.ToString();
                changesDetected = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile image");
                return BadRequest("Failed to update profile image");
            }
        }

        // 7. Save Changes
        if (changesDetected)
        {
            var saveResult = await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} changes to database", saveResult);
            
            await transaction.CommitAsync();
            
            var newToken = _tokenService.GenerateToken(user);
            _logger.LogInformation("Generated new JWT token for user");

            return Ok(new ProfileUpdateResponse
            {
                Message = "Profile updated successfully",
                Token = newToken,
                AvatarUrl = GetFullImageUrl(user.ProfileImageUrl),
                Username = user.Username
            });
        }

        _logger.LogInformation("No changes detected for user {UserId}", user.Id);
        return Ok(new { Message = "No changes detected" });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Failed to update profile");
        return StatusCode(500, "An error occurred while updating your profile");
    }
}

// --- Helper Methods --- //

    private string GetPublicIdFromUrl(string url)
    {
        var uri = new Uri(url);
        return Path.GetFileNameWithoutExtension(uri.AbsolutePath.Split('/').Last());
    }

    private async Task<string?> UpdatePassword(User user, string currentPassword, string newPassword)
    {
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword)
            != PasswordVerificationResult.Success)
            return "Current password is incorrect.";

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        return null;
    }
    private async Task<bool> IsUsernameTaken(string newUsername, string currentUsername)
        => newUsername != currentUsername
           && await _context.Users.AnyAsync(u => u.Username == newUsername);

    private async Task<bool> IsEmailTaken(string newEmail, string currentEmail)
        => newEmail != currentEmail
           && await _context.Users.AnyAsync(u => u.Email == newEmail);

    private async Task SendVerificationEmail(User user)
    {
        var frontendBaseUrl = "https://matchboxd-frontend.vercel.app";
        var verificationLink = $"{frontendBaseUrl}/verify-email?token={user.VerificationToken}";
        await _emailService.SendVerificationEmailAsync(user.Email, user.Username, verificationLink);
    }
    //
    // private async Task<(bool Success, string? FileUrl, string? Error)> UploadProfileImage(
    //     IFormFile file, string? existingImagePath)
    // {
    //     // Validate file
    //     var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    //     var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    //
    //     if (!allowedExtensions.Contains(extension))
    //         return (false, null, "Invalid file type. Only JPG/PNG/GIF allowed.");
    //
    //     if (file.Length > 5 * 1024 * 1024) // 5MB
    //         return (false, null, "File size exceeds 5MB limit.");
    //
    //     if (!string.IsNullOrEmpty(existingImagePath))
    //     {
    //         var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingImagePath.TrimStart('/'));
    //         if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
    //     }
    //
    //     var configuration = new ConfigurationBuilder()
    //         .SetBasePath(Directory.GetCurrentDirectory())
    //         .AddJsonFile("appsettings.json")
    //         .Build();
    //
    //     var cloudinaryAccount = new Account(
    //         configuration["Cloudinary:CloudName"],
    //         configuration["Cloudinary:ApiKey"],
    //         configuration["Cloudinary:ApiSecret"]);
    //
    //     var cloudinary = new Cloudinary(cloudinaryAccount);
    //
    //     var uploadParams = new ImageUploadParams()
    //     {
    //         File = new FileDescription(file.FileName, file.OpenReadStream()),
    //         PublicId = $"profiles/{Guid.NewGuid()}",
    //         Folder = "matchboxd/profiles" // Optional: better organization
    //     };
    //
    //     try
    //     {
    //         var uploadResult = await cloudinary.UploadAsync(uploadParams);
    //     
    //         if (uploadResult.Error != null)
    //         {
    //             _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
    //             return (false, null, "Image upload failed");
    //         }
    //
    //         return (true, uploadResult.SecureUrl.ToString(), null);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Cloudinary upload exception");
    //         return (false, null, "Image upload failed");
    //     }
    // }

    private string GetFullImageUrl(string? relativePath)
        => relativePath != null
            ? $"{Request.Scheme}://{Request.Host}{relativePath}"
            : null;
}