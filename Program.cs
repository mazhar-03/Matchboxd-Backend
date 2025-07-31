using System.Security.Claims;
using System.Text;
using Matchboxd.API.DAL;
using Matchboxd.API.Helpers.Options;
using Matchboxd.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var config = builder.Configuration;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("https://matchboxd-frontend.vercel.app")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddHttpClient("FootballData", client =>
{
    client.BaseAddress = new Uri(config["FootballDataApi:BaseUrl"] ?? string.Empty);
    client.DefaultRequestHeaders.Add("X-Auth-Token", config["FootballDataApi:ApiKey"]);
});

var con = builder.Configuration.GetConnectionString("DefaultConnection")
          ?? throw new Exception("Default connection string is not found!");

var jwtConfigData = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtConfigData);

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<EmailService>();

// Add health check services
builder.Services.AddHealthChecks()
    .AddNpgSql(con, name: "postgresql", tags: new[] { "database" });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfigData["Issuer"],
            ValidAudience = jwtConfigData["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigData["Key"])),
            ClockSkew = TimeSpan.FromMinutes(20),

            // I tried everything but everytime I got Unauthorized. Without that i did not make it to recognize the username for the system.
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(con));

builder.Services.AddScoped<MatchImportService>();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

var app = builder.Build();

// Add this near the top of your Program.cs
var storagePath = Environment.GetEnvironmentVariable("RENDER_STORAGE_PATH") ?? "wwwroot";
var uploadsPath = Path.Combine(storagePath, "uploads");

// Ensure the uploads directory exists
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Configure static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles(); 

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to Matchboxd API!");

// Enhanced health check
app.MapHealthChecks("/api/health", new HealthCheckOptions {
    ResponseWriter = async (context, report) => {
        await context.Response.WriteAsJsonAsync(new {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(e => new {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration
            })
        });
    }
});

app.Run();