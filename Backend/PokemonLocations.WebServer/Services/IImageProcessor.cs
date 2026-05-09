using SkiaSharp;

namespace PokemonLocations.WebServer.Services;

public interface IImageProcessor {
    Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct);
}

public record ProcessedImage(
    byte[] Bytes,
    SKEncodedImageFormat Format,
    int Width,
    int Height);

public class UnsupportedFormatException : Exception { }
public class DecodeFailedException : Exception { }
public class DecodeBombException : Exception { }
