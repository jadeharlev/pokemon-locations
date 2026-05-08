using SkiaSharp;

namespace PokemonLocations.WebServer.Services;

public class ImageProcessor : IImageProcessor {
    private const int ResizeLongestEdge = 2000;
    private const long MaxPixels = 50_000_000;
    private const int Quality = 85;

    public Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct) {
        using var managed = new SKManagedStream(input);
        using var codec = SKCodec.Create(managed);
        if (codec is null) throw new DecodeFailedException();

        var format = codec.EncodedFormat;
        if (format is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp)) {
            throw new UnsupportedFormatException();
        }

        var info = codec.Info;
        if ((long)info.Width * info.Height > MaxPixels) throw new DecodeBombException();

        using var sourceBitmap = SKBitmap.Decode(codec);
        if (sourceBitmap is null) throw new DecodeFailedException();

        SKBitmap? resizedBitmap = null;
        try {
            var workingBitmap = sourceBitmap;
            var longest = Math.Max(sourceBitmap.Width, sourceBitmap.Height);
            if (longest > ResizeLongestEdge) {
                var scale = (double)ResizeLongestEdge / longest;
                var newW = (int)(sourceBitmap.Width * scale);
                var newH = (int)(sourceBitmap.Height * scale);
                resizedBitmap = sourceBitmap.Resize(
                    new SKImageInfo(newW, newH),
                    new SKSamplingOptions(SKCubicResampler.Mitchell));
                if (resizedBitmap is null) throw new DecodeFailedException();
                workingBitmap = resizedBitmap;
            }

            using var image = SKImage.FromBitmap(workingBitmap);
            using var data = image.Encode(format, Quality);
            return Task.FromResult(new ProcessedImage(
                Bytes: data.ToArray(),
                Format: format,
                Width: workingBitmap.Width,
                Height: workingBitmap.Height));
        } finally {
            resizedBitmap?.Dispose();
        }
    }
}
