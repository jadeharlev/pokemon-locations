namespace PokemonLocations.WebServer.Models;

public record UserImage(
    Guid ImageId,
    int UserId,
    int LocationId,
    string FilePath,
    string OriginalFilename,
    string ContentType,
    int ByteSize,
    DateTime UploadedAt);
