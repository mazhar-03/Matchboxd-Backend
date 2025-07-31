using System.Text.RegularExpressions;
using Matchboxd.API.DAL;
using Matchboxd.API.Dtos;
using Matchboxd.API.Models;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchboxd.API.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext context, ITokenService tokenService, EmailService emailService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Username == loginDto.Username);

        if (user == null) return Unauthorized("User not found.");
        if (!user.EmailVerified) return Unauthorized("Email not verified.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (result == PasswordVerificationResult.Failed) 
            return Unauthorized("Invalid password.");

        var token = _tokenService.GenerateToken(user);

        
        return Ok(new { 
            message = "Login successful", 
            token, // Still return in body for client-side use if needed
            username = user.Username,
            userPhoto = user.ProfileImageUrl
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // Validate email
        var emailError = Validation.ValidateEmail(dto.Email);
        if (emailError != null)
            return BadRequest(emailError);

        // Validate username
        var usernameError = Validation.ValidateUsername(dto.Username);
        if (usernameError != null)
            return BadRequest(usernameError);

        // Validate password
        var passwordError = Validation.ValidatePassword(dto.Password);
        if (passwordError != null)
            return BadRequest(passwordError);

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email is already registered.");

        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest("Username is already taken.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.HashPassword(null!, dto.Password),
            EmailVerified = false,
            VerificationToken = TokenGenerator.GenerateVerificationToken(),
            VerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var frontendBaseUrl = "https://matchboxd-frontend-ej4biv7ps-mazhar-altincays-projects.vercel.app";
        var verificationLink = $"{frontendBaseUrl}/verify-email?token={user.VerificationToken}";

        await _emailService.SendVerificationEmailAsync(user.Email, user.Username, verificationLink);

        return Ok("Registration successful! Please check your email to verify your account.");
    }


    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (string.IsNullOrEmpty(dto.Token))
            return BadRequest(new { message = "Token is missing." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == dto.Token);
        if (user == null)
            return BadRequest(new { message = "Invalid token." });

        if (user.VerificationTokenExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Token expired." });

        user.EmailVerified = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiry = null;

        Console.WriteLine("Incoming verification token: " + dto.Token);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Email verified successfully!\nYou can close this page." });
    }


    [HttpPost("resend-verification-email")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound("User not found.");

        if (user.EmailVerified)
            return BadRequest("Email already verified.");

        user.VerificationToken = TokenGenerator.GenerateVerificationToken();
        user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

        await _context.SaveChangesAsync();

        var frontendBaseUrl = "https://matchboxd-frontend-ej4biv7ps-mazhar-altincays-projects.vercel.app";
        var verificationLink = $"{frontendBaseUrl}/verify-email?token={user.VerificationToken}";

        await _emailService.SendVerificationEmailAsync(user.Email, user.Username, verificationLink);

        return Ok("Verification email resent.");
    }
}