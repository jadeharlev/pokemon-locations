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
}
