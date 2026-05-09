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

    [Fact]
    public async Task ResizesLandscapeOversizedImageToLongestEdge2000() {
        var input = TestImageFixtures.CreatePng(4000, 3000);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(2000, result.Width);
        Assert.Equal(1500, result.Height);
    }

    [Fact]
    public async Task ResizesPortraitOversizedImageHeightDriven() {
        var input = TestImageFixtures.CreatePng(3000, 4000);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(1500, result.Width);
        Assert.Equal(2000, result.Height);
    }

    [Fact]
    public async Task DoesNotResizeWhenLongestEdgeUnderThreshold() {
        var input = TestImageFixtures.CreatePng(1500, 1000);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(1500, result.Width);
        Assert.Equal(1000, result.Height);
    }

    [Fact]
    public async Task DoesNotResizeAtExactBoundary() {
        var input = TestImageFixtures.CreatePng(2000, 1500);
        var result = await processor.ProcessAsync(new MemoryStream(input), default);

        Assert.Equal(2000, result.Width);
        Assert.Equal(1500, result.Height);
    }

    [Fact]
    public async Task RejectsUnsupportedFormat() {
        var input = TestImageFixtures.CreateGif();
        await Assert.ThrowsAsync<UnsupportedFormatException>(
            () => processor.ProcessAsync(new MemoryStream(input), default));
    }

    [Fact]
    public async Task RejectsDecodeBombByPixelCount() {
        // 8000x8000 = 64 MP, exceeds 50 MP cap. Pattern compresses to small file.
        var input = TestImageFixtures.CreatePng(8000, 8000);
        await Assert.ThrowsAsync<DecodeBombException>(
            () => processor.ProcessAsync(new MemoryStream(input), default));
    }

    [Fact]
    public async Task RejectsCorruptBytes() {
        var input = TestImageFixtures.CreateCorruptBytes();
        await Assert.ThrowsAsync<DecodeFailedException>(
            () => processor.ProcessAsync(new MemoryStream(input), default));
    }
}
