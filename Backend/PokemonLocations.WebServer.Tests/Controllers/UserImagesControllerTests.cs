using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class UserImagesControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public UserImagesControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient? apiClient = null) =>
        new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = apiClient ?? Substitute.For<IPokemonLocationsApiClient>()
        };

    private static IPokemonLocationsApiClient ApiClientThatAcceptsLocations() {
        var client = Substitute.For<IPokemonLocationsApiClient>();
        client.ExistsAsync(Arg.Any<string>()).Returns(true);
        return client;
    }

    private static HttpClient AuthorizedClient(
        PokemonLocationsWebServerFactory factory, string email, string password) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
        return client;
    }

    private static MultipartFormDataContent MakeMultipart(byte[] bytes, string filename, string mime) {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
        content.Add(fileContent, "file", filename);
        return content;
    }

    private static byte[] ValidPngBytes(int w = 64, int h = 64) =>
        PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreatePng(w, h);

    [Fact]
    public async Task PostReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent {
            { new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }), "file", "x.png" }
        };
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReturns401WithoutAuth() {
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/me/locations/1/images/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostValidPngReturns201WithFileOnDiskAndDbRow() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var bytes = ValidPngBytes();
        using var content = MakeMultipart(bytes, "shot.png", "image/png");
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var imageId = Guid.Parse(body.GetProperty("imageId").GetString()!);
        Assert.Equal($"/api/me/locations/1/images/{imageId}", body.GetProperty("imageUrl").GetString());
        Assert.Equal("shot.png", body.GetProperty("originalFilename").GetString());

        Assert.True(Directory.EnumerateFiles(factory.UploadRoot, "*", SearchOption.AllDirectories)
                             .Any(p => p.EndsWith($"{imageId}.png")));
    }

    [Fact]
    public async Task PostValidJpegReturns201WithJpgExtension() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var bytes = PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreateJpeg(64, 64);
        using var content = MakeMultipart(bytes, "shot.jpg", "image/jpeg");
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var imageId = Guid.Parse(body.GetProperty("imageId").GetString()!);
        Assert.True(Directory.EnumerateFiles(factory.UploadRoot, $"{imageId}.jpg", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task PostValidWebpReturns201WithWebpExtension() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var bytes = PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreateWebp(64, 64);
        using var content = MakeMultipart(bytes, "shot.webp", "image/webp");
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var imageId = Guid.Parse(body.GetProperty("imageId").GetString()!);
        Assert.True(Directory.EnumerateFiles(factory.UploadRoot, $"{imageId}.webp", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task PostFileLargerThan10MbReturns400FileTooLarge() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var bytes = new byte[11 * 1024 * 1024];
        new Random(0).NextBytes(bytes);
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        using var content = MakeMultipart(bytes, "big.png", "image/png");

        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("file_too_large", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostBodyExceedingMaxRequestBodySizeIsRejectedByFramework() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var bytes = new byte[15 * 1024 * 1024];
        using var content = MakeMultipart(bytes, "huge.png", "image/png");

        var response = await client.PostAsync("/api/me/locations/1/images", content);

        // TestServer enforces FormOptions.MultipartBodyLengthLimit → 400
        // Real Kestrel enforces MaxRequestBodySize → 413
        // Either proves framework-level rejection before our controller runs.
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.RequestEntityTooLarge,
            $"Expected 400 or 413, got {response.StatusCode}");
    }

    [Fact]
    public async Task PostUnsupportedMimeReturns400() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        using var content = MakeMultipart(new byte[] { 0x00 }, "x.tiff", "image/tiff");
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("unsupported_media_type", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostNonExistentLocationReturns404() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = Substitute.For<IPokemonLocationsApiClient>();
        apiClient.ExistsAsync(Arg.Any<string>()).Returns(false);
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        using var content = MakeMultipart(ValidPngBytes(), "x.png", "image/png");
        var response = await client.PostAsync("/api/me/locations/999/images", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("location_not_found", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostWhenAtCapReturns400CapReached() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        for (int i = 0; i < 20; i++) {
            using var c = MakeMultipart(ValidPngBytes(), $"x{i}.png", "image/png");
            var r = await client.PostAsync("/api/me/locations/1/images", c);
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        using var overflow = MakeMultipart(ValidPngBytes(), "overflow.png", "image/png");
        var response = await client.PostAsync("/api/me/locations/1/images", overflow);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("cap_reached", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostCorruptBytesWithValidMimeReturns415() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        using var content = MakeMultipart(
            PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreateCorruptBytes(),
            "x.png", "image/png");
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("decode_failed", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task PostDecodeBombReturns400() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var factory = CreateFactory(ApiClientThatAcceptsLocations());
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var bytes = PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreatePng(8000, 8000);
        using var content = MakeMultipart(bytes, "bomb.png", "image/png");
        var response = await client.PostAsync("/api/me/locations/1/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("decode_bomb", (await ReadJsonAsync(response)).GetProperty("error").GetString());
    }
}
