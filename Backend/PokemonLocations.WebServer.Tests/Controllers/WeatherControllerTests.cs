using System.Net;
using System.Text.Json;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Tests.Infrastructure;
using static PokemonLocations.WebServer.Tests.Infrastructure.TestHelpers;

namespace PokemonLocations.WebServer.Tests.Controllers;

[Collection("WebServer3")]
public class WeatherControllerTests {
    private readonly WebServerFixture fixture;

    public WeatherControllerTests(WebServerFixture fixture) {
        this.fixture = fixture;
    }

    private static Planet TestVulcan() => new(
        Name: "Vulcan",
        SolarSystem: "40 Eridani",
        AtmosphericPressure: 1.0,
        MaxTemp: 50,
        MinTemp: 10,
        Description: "Hot desert planet.",
        ImageUrl: "/images/planets/vulcan.jpg");

    private PokemonLocationsWebServerFactory CreateFactory(FakeStarTrekWeatherApiClient weather) {
        fixture.Factory.Overrides.Current = new TestServiceOverrides { WeatherClient = weather };
        return fixture.Factory;
    }

    private async Task<HttpClient> AuthorizedClientAsync(PokemonLocationsWebServerFactory factory) {
        await ResetUsersAsync(fixture.PostgresConnectionString);
        await SeedUserAsync(fixture.PostgresConnectionString, "red@example.com", "pikachu123", "Red");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = BasicHeader("red@example.com", "pikachu123");
        return client;
    }

    [Fact]
    public async Task GetAllReturns401WithoutBasicHeader() {
        var factory = CreateFactory(new FakeStarTrekWeatherApiClient());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/planets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllReturnsPlanetsFromUpstream() {
        var weather = new FakeStarTrekWeatherApiClient(TestVulcan());
        var factory = CreateFactory(weather);
        var client = await AuthorizedClientAsync(factory);

        var response = await client.GetAsync("/api/planets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("Vulcan", body[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetByNameReturns404WhenUpstreamMissing() {
        var weather = new FakeStarTrekWeatherApiClient();
        var factory = CreateFactory(weather);
        var client = await AuthorizedClientAsync(factory);

        var response = await client.GetAsync("/api/planets/Vulcan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetImageReturns401WithoutBasicHeader() {
        var factory = CreateFactory(new FakeStarTrekWeatherApiClient());
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/planets/images/vulcan.jpg");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetImageReturnsBytesAndContentTypeFromUpstream() {
        var weather = new FakeStarTrekWeatherApiClient();
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03 };
        weather.Images["vulcan.jpg"] = (bytes, "image/jpeg");
        var factory = CreateFactory(weather);
        var client = await AuthorizedClientAsync(factory);

        var response = await client.GetAsync("/api/planets/images/vulcan.jpg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        var actual = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, actual);
    }

    [Fact]
    public async Task GetImageReturns404WhenUpstreamMissing() {
        var weather = new FakeStarTrekWeatherApiClient();
        var factory = CreateFactory(weather);
        var client = await AuthorizedClientAsync(factory);

        var response = await client.GetAsync("/api/planets/images/notreal.jpg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("..%2Fappsettings.json")]
    [InlineData("foo%2Fbar.jpg")]
    [InlineData("foo%5Cbar.jpg")]
    [InlineData("foo%20bar.jpg")]
    [InlineData("..jpg")]
    public async Task GetImageReturns400ForUnsafeFileName(string encodedFileName) {
        var weather = new FakeStarTrekWeatherApiClient();
        var factory = CreateFactory(weather);
        var client = await AuthorizedClientAsync(factory);

        var response = await client.GetAsync($"/api/planets/images/{encodedFileName}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
