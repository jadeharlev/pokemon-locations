using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Models.Responses;

public record UploadedImageResponse(
    Guid ImageId,
    string ImageUrl,
    string OriginalFilename,
    DateTimeOffset UploadedAt) {
    public static UploadedImageResponse FromDomain(UserImage image) =>
        new(
            image.ImageId,
            $"/api/me/locations/{image.LocationId}/images/{image.ImageId}",
            image.OriginalFilename,
            image.UploadedAt);
}
