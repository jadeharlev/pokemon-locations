using PokemonLocations.WebServer.Services;
using SkiaSharp;

namespace PokemonLocations.WebServer.Tests.Imaging;

public class ImageProcessorTests {
    private readonly ImageProcessor processor = new();

    [Fact]
    public async Task ProcessesPngAndPreservesFormat() {
        var input = TestImageFixtures.CreatePng(300, 200);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(SKEncodedImageFormat.Png, result.Format);
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
        Assert.NotEmpty(result.Bytes);
    }

    [Fact]
    public async Task ProcessesJpegAndPreservesFormat() {
        var input = TestImageFixtures.CreateJpeg(300, 200);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(SKEncodedImageFormat.Jpeg, result.Format);
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public async Task ProcessesWebpAndPreservesFormat() {
        var input = TestImageFixtures.CreateWebp(300, 200);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(SKEncodedImageFormat.Webp, result.Format);
        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
    }
}
