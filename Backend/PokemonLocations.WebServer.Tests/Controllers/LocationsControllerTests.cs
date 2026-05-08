using System.Net;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("PostgresAndRedis")]
public class LocationsControllerTests {
    private readonly PostgresFixture postgresFixture;
    private readonly RedisFixture redisFixture;

    public LocationsControllerTests(PostgresFixture postgresFixture, RedisFixture redisFixture) {
        this.postgresFixture = postgresFixture;
        this.redisFixture = redisFixture;
    }

    private PokemonLocationsWebServerFactory CreateFactory(IPokemonLocationsApiClient apiClient) =>
        new(postgresFixture.ConnectionString, redisFixture.ConnectionString) {
            ApiClient = apiClient
        };

    private static HttpClient AuthorizedClient(
        PokemonLocationsWebServerFactory factory, string email, string password) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader(email, password);
        return client;
    }

    private static IPokemonLocationsApiClient CreateApiClient() {
        var client = Substitute.For<IPokemonLocationsApiClient>();
        client.ExistsAsync(Arg.Any<string>()).Returns(true);
        return client;
    }

    private const string TwoLocationsJson =
        """[{"locationId":1,"name":"Pallet Town","description":"A quiet town.","videoUrl":null},{"locationId":2,"name":"Viridian City","description":"A green city.","videoUrl":null}]""";

    private const string SingleLocationJson =
        """{"locationId":1,"name":"Pallet Town","description":"A quiet town.","videoUrl":null,"images":[{"imageId":1,"locationId":1,"imageUrl":"/images/pallet-town-overview.png","displayOrder":1,"caption":"Pallet Town overview"}]}""";

    private const string SingleLocationJsonNoImages =
        """{"locationId":2,"name":"Viridian City","description":"A green city.","videoUrl":null,"images":[]}""";

    // --- 401 tests ---

    [Fact]
    public async Task GetAllReturns401WithoutAuth() {
        var factory = CreateFactory(CreateApiClient());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByIdReturns401WithoutAuth() {
        var factory = CreateFactory(CreateApiClient());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/locations/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- GET /api/locations ---

    [Fact]
    public async Task GetAllProxiesApiResponse() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations").Returns(new ApiResponse(200, TwoLocationsJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(2, body.GetArrayLength());
        Assert.Equal("Pallet Town", body[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAllMergesVisitedFlag() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations").Returns(new ApiResponse(200, TwoLocationsJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        // Mark a building in location 1 as visited; that should mark location 1 as visited too.
        await client.PutAsync("/api/me/visited/buildings/1/100", content: null);

        var response = await client.GetAsync("/api/locations");
        var body = await ReadJsonAsync(response);

        Assert.True(body[0].GetProperty("visited").GetBoolean());
        Assert.False(body[1].GetProperty("visited").GetBoolean());
    }

    [Fact]
    public async Task GetAllPropagatesApi404() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations").Returns(new ApiResponse(404, null));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GET /api/locations/{id} ---

    [Fact]
    public async Task GetByIdProxiesAndMergesVisited() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        // Mark a building in location 1 as visited; that should mark location 1 as visited too.
        await client.PutAsync("/api/me/visited/buildings/1/100", content: null);

        var response = await client.GetAsync("/api/locations/1");
        var body = await ReadJsonAsync(response);

        Assert.Equal("Pallet Town", body.GetProperty("name").GetString());
        Assert.True(body.GetProperty("visited").GetBoolean());
    }

    [Fact]
    public async Task GetByIdReturnsEmptyUserImagesForNewUser() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/1");
        var body = await ReadJsonAsync(response);

        Assert.Equal(0, body.GetProperty("userImages").GetArrayLength());
    }

    [Fact]
    public async Task GetByIdIncludesUploadedUserImages() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        using var content = new MultipartFormDataContent();
        var fc = new ByteArrayContent(PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreatePng(64, 64));
        fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fc, "file", "shot.png");
        await client.PostAsync("/api/me/locations/1/images", content);

        var response = await client.GetAsync("/api/locations/1");
        var body = await ReadJsonAsync(response);
        var ui = body.GetProperty("userImages");

        Assert.Equal(1, ui.GetArrayLength());
        Assert.Equal("shot.png", ui[0].GetProperty("originalFilename").GetString());
        Assert.StartsWith("/api/me/locations/1/images/", ui[0].GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task GetByIdDoesNotLeakAnotherUsersImages() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        await SeedUserAsync(postgresFixture.ConnectionString, "blue@example.com", "squirtle1", "Blue");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
        var factory = CreateFactory(apiClient);

        var blueClient = AuthorizedClient(factory, "blue@example.com", "squirtle1");
        using var content = new MultipartFormDataContent();
        var fc = new ByteArrayContent(PokemonLocations.WebServer.Tests.Imaging.TestImageFixtures.CreatePng(64, 64));
        fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fc, "file", "blueshot.png");
        await blueClient.PostAsync("/api/me/locations/1/images", content);

        var redClient = AuthorizedClient(factory, "red@example.com", "pikachu123");
        var response = await redClient.GetAsync("/api/locations/1");
        var body = await ReadJsonAsync(response);

        Assert.Equal(0, body.GetProperty("userImages").GetArrayLength());
    }

    [Fact]
    public async Task GetByIdPassesCanonicalImagesThrough() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/1").Returns(new ApiResponse(200, SingleLocationJson));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/1");
        var body = await ReadJsonAsync(response);

        var images = body.GetProperty("images");
        Assert.Equal(1, images.GetArrayLength());
        Assert.Equal("/images/pallet-town-overview.png", images[0].GetProperty("imageUrl").GetString());
        Assert.Equal("Pallet Town overview", images[0].GetProperty("caption").GetString());
    }

    [Fact]
    public async Task GetByIdPassesEmptyImagesArrayThrough() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/2").Returns(new ApiResponse(200, SingleLocationJsonNoImages));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/2");
        var body = await ReadJsonAsync(response);

        Assert.Equal(0, body.GetProperty("images").GetArrayLength());
    }

    [Fact]
    public async Task GetByIdPropagatesApi404() {
        await ResetUsersAsync(postgresFixture.ConnectionString);
        await SeedUserAsync(postgresFixture.ConnectionString, "red@example.com", "pikachu123", "Red");
        var apiClient = CreateApiClient();
        apiClient.GetWithStatusAsync("/locations/999").Returns(new ApiResponse(404, null));
        var factory = CreateFactory(apiClient);
        var client = AuthorizedClient(factory, "red@example.com", "pikachu123");

        var response = await client.GetAsync("/api/locations/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
