using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Models.Responses;
using PokemonLocations.WebServer.Services;
using SkiaSharp;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/locations/{locationId:int}/images")]
public class UserImagesController : ControllerBase {
    private readonly IUserImageRepository repository;
    private readonly IImageProcessor processor;
    private readonly IPokemonLocationsApiClient apiClient;
    private readonly UserImagesOptions options;
    private readonly ILogger<UserImagesController> logger;

    public UserImagesController(
        IUserImageRepository repository,
        IImageProcessor processor,
        IPokemonLocationsApiClient apiClient,
        IOptions<UserImagesOptions> options,
        ILogger<UserImagesController> logger) {
        this.repository = repository;
        this.processor = processor;
        this.apiClient = apiClient;
        this.options = options.Value;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int locationId, IFormFile file, CancellationToken ct) {
        var userId = User.GetUserId();

        if (!await apiClient.ExistsAsync($"/locations/{locationId}")) {
            return NotFound(new { error = "location_not_found" });
        }

        var mime = file.ContentType?.ToLowerInvariant();
        if (mime is not ("image/png" or "image/jpeg" or "image/webp")) {
            return BadRequest(new { error = "unsupported_media_type" });
        }

        if (file.Length > options.MaxBytesPerFile) {
            return BadRequest(new { error = "file_too_large" });
        }

        var current = await repository.CountForUserAndLocationAsync(userId, locationId);
        if (current >= options.MaxFilesPerLocation) {
            return BadRequest(new { error = "cap_reached" });
        }

        ProcessedImage processed;
        try {
            await using var stream = file.OpenReadStream();
            processed = await processor.ProcessAsync(stream, ct);
        } catch (UnsupportedFormatException) {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = "decode_failed" });
        } catch (DecodeFailedException) {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { error = "decode_failed" });
        } catch (DecodeBombException) {
            return BadRequest(new { error = "decode_bomb" });
        }

        var uuid = Guid.NewGuid();
        var ext = ExtensionFor(processed.Format);
        var userDir = Path.Combine(options.UploadRoot, userId.ToString());
        Directory.CreateDirectory(userDir);
        var tempPath = Path.Combine(userDir, $"{uuid}.tmp");
        var finalPath = Path.Combine(userDir, $"{uuid}.{ext}");
        await System.IO.File.WriteAllBytesAsync(tempPath, processed.Bytes, ct);
        System.IO.File.Move(tempPath, finalPath);

        var image = new UserImage(
            ImageId: uuid,
            UserId: userId,
            LocationId: locationId,
            FilePath: finalPath,
            OriginalFilename: file.FileName,
            ContentType: ContentTypeFor(processed.Format),
            ByteSize: processed.Bytes.Length,
            UploadedAt: DateTime.UtcNow);

        try {
            var result = await repository.AddAsync(image, options.MaxFilesPerLocation);
            if (result == AddResult.AtCap) {
                DeleteSilently(finalPath);
                return BadRequest(new { error = "cap_reached" });
            }
        } catch (Npgsql.PostgresException ex) when (ex.SqlState == "40001") {
            try {
                var retry = await repository.AddAsync(image, options.MaxFilesPerLocation);
                if (retry == AddResult.AtCap) {
                    DeleteSilently(finalPath);
                    return BadRequest(new { error = "cap_reached" });
                }
            } catch (Npgsql.PostgresException) {
                DeleteSilently(finalPath);
                return Conflict(new { error = "serialization_conflict" });
            }
        }

        return Created(
            $"/api/me/locations/{locationId}/images/{uuid}",
            UploadedImageResponse.FromDomain(image));
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(int locationId, Guid imageId) {
        var userId = User.GetUserId();
        var image = await repository.GetByIdForUserAsync(userId, imageId);
        if (image is null || image.LocationId != locationId) {
            return NotFound(new { error = "not_found" });
        }
        await repository.RemoveAsync(userId, imageId);
        DeleteSilently(image.FilePath);
        return NoContent();
    }

    [HttpGet("{imageId:guid}")]
    public Task<IActionResult> Get(int locationId, Guid imageId) =>
        throw new NotImplementedException();

    private void DeleteSilently(string path) {
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to delete orphaned upload {Path}", path); }
    }

    private static string ExtensionFor(SKEncodedImageFormat fmt) => fmt switch {
        SKEncodedImageFormat.Png => "png",
        SKEncodedImageFormat.Jpeg => "jpg",
        SKEncodedImageFormat.Webp => "webp",
        _ => throw new InvalidOperationException("Unsupported format reached file write")
    };

    private static string ContentTypeFor(SKEncodedImageFormat fmt) => fmt switch {
        SKEncodedImageFormat.Png => "image/png",
        SKEncodedImageFormat.Jpeg => "image/jpeg",
        SKEncodedImageFormat.Webp => "image/webp",
        _ => throw new InvalidOperationException("Unsupported format reached content-type")
    };
}
