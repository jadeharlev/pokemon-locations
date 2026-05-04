using System.Net;
using System.Net.Http.Json;
using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Api;

[Collection("Postgres")]
public class LocationImagesEndpointsTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private PokemonLocationsApiFactory factory = null!;
    private HttpClient client = null!;

    public LocationImagesEndpointsTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    public async Task InitializeAsync() {
        await using (var conn = new NpgsqlConnection(postgres.ConnectionString)) {
            await conn.OpenAsync();
            await conn.ExecuteAsync("TRUNCATE TABLE locations RESTART IDENTITY CASCADE");
        }

        factory = new PokemonLocationsApiFactory(postgres.ConnectionString);
        client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync() {
        client?.Dispose();
        factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<int> SeedLocationAsync(string name = "Test Town") {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = name });
    }

    private async Task<int> SeedImageAsync(
        int locationId,
        string imageUrl = "/images/test.png",
        int displayOrder = 0,
        string? caption = null) {
        await using var conn = new NpgsqlConnection(postgres.ConnectionString);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO location_images (location_id, image_url, display_order, caption)
              VALUES (@LocationId, @ImageUrl, @DisplayOrder, @Caption)
              RETURNING image_id",
            new { LocationId = locationId, ImageUrl = imageUrl, DisplayOrder = displayOrder, Caption = caption });
    }

    [Fact]
    public async Task GetAllImagesReturnsNotFoundWhenLocationDoesNotExist() {
        var response = await client.GetAsync("/locations/999999/images");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllImagesReturnsEmptyArrayWhenLocationHasNoImages() {
        var locationId = await SeedLocationAsync();

        var response = await client.GetAsync($"/locations/{locationId}/images");

        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<List<LocationImage>>();
        Assert.NotNull(images);
        Assert.Empty(images!);
    }

    [Fact]
    public async Task GetAllImagesReturnsSeededImagesOrderedByDisplayOrder() {
        var locationId = await SeedLocationAsync();
        await SeedImageAsync(locationId, "/images/c.png", 3, "C");
        await SeedImageAsync(locationId, "/images/a.png", 1, "A");
        await SeedImageAsync(locationId, "/images/b.png", 2, "B");

        var response = await client.GetAsync($"/locations/{locationId}/images");

        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<List<LocationImage>>();
        Assert.NotNull(images);
        Assert.Equal(new[] { 1, 2, 3 }, images!.Select(i => i.DisplayOrder));
    }

    [Theory]
    [InlineData("POST", "/locations/1/images")]
    [InlineData("PUT", "/locations/1/images/1")]
    [InlineData("DELETE", "/locations/1/images/1")]
    public async Task WriteVerbsReturnMethodNotAllowed(string method, string path) {
        var request = new HttpRequestMessage(new HttpMethod(method), path) {
            Content = JsonContent.Create(new LocationImage { ImageUrl = "/x.png" })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
