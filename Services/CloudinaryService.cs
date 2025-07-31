using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Matchboxd.API.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]);

        _cloudinary = new Cloudinary(account);
        _logger = logger;
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = Guid.NewGuid().ToString(),
            Overwrite = false,
            Transformation = new Transformation()
                .Width(500).Height(500).Crop("fill").Gravity("face")
        };

        try
        {
            var result = await _cloudinary.UploadAsync(uploadParams);
            _logger.LogInformation("Uploaded image to Cloudinary: {PublicId}", result.PublicId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image to Cloudinary");
            throw;
        }
    }
    
    public async Task DeleteImageAsync(string publicId)
    {
        try
        {
            await _cloudinary.DeleteResourcesAsync(publicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Cloudinary image: {PublicId}", publicId);
            throw;
        }
    }
}