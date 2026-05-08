using SkiaSharp;

namespace PokemonLocations.WebServer.Tests.Imaging;

public static class TestImageFixtures {
    public static byte[] CreateImage(int width, int height, SKEncodedImageFormat format, int quality = 90) {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        // Draw a simple pattern so the image isn't pure-solid (PNG zlib compresses too aggressively)
        using var paint = new SKPaint { Color = SKColors.White };
        for (int y = 0; y < height; y += 32) canvas.DrawLine(0, y, width, y, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        return data.ToArray();
    }

    public static byte[] CreatePng(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Png, 100);

    public static byte[] CreateJpeg(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Jpeg, 85);

    public static byte[] CreateWebp(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Webp, 85);

    public static byte[] CreateGif(int width = 1, int height = 1) {
        // 35-byte transparent 1x1 GIF; SkiaSharp can decode but we don't accept GIF.
        return new byte[] {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B
        };
    }

    public static byte[] CreateCorruptBytes() =>
        new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
}
