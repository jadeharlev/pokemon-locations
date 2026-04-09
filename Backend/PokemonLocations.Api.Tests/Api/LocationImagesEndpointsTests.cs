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
        client = factory.CreateClient();
    }

    public Task DisposeAsync() {
        client?.Dispose();
        factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<int> CreateLocationAsync(string name = "Test Town") {
        var response = await client.PostAsJsonAsync("/locations", new Location { Name = name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Location>();
        return created!.LocationId;
    }

    [Fact]
    public async Task GetAllImagesReturnsNotFoundWhenLocationDoesNotExist() {
        var response = await client.GetAsync("/locations/999999/images");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllImagesReturnsEmptyArrayWhenLocationHasNoImages() {
        var locationId = await CreateLocationAsync();

        var response = await client.GetAsync($"/locations/{locationId}/images");

        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<List<LocationImage>>();
        Assert.NotNull(images);
        Assert.Empty(images!);
    }

    [Fact]
    public async Task PostImageReturnsCreated() {
        var locationId = await CreateLocationAsync();

        var response = await client.PostAsJsonAsync($"/locations/{locationId}/images", new LocationImage {
            ImageUrl = "/images/overview.png",
            DisplayOrder = 1,
            Caption = "Overview"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LocationImage>();
        Assert.NotNull(created);
        Assert.True(created!.ImageId > 0);
        Assert.Equal(locationId, created.LocationId);
    }

    [Fact]
    public async Task PostImageReturnsNotFoundWhenLocationDoesNotExist() {
        var response = await client.PostAsJsonAsync("/locations/999999/images", new LocationImage {
            ImageUrl = "/images/x.png"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostImageWithoutImageUrlReturnsBadRequest() {
        var locationId = await CreateLocationAsync();

        var response = await client.PostAsJsonAsync($"/locations/{locationId}/images", new LocationImage {
            ImageUrl = string.Empty
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutUpdatesExistingImage() {
        var locationId = await CreateLocationAsync();
        var post = await client.PostAsJsonAsync($"/locations/{locationId}/images", new LocationImage {
            ImageUrl = "/images/old.png",
            DisplayOrder = 1,
            Caption = "Old"
        });
        var created = await post.Content.ReadFromJsonAsync<LocationImage>();

        var put = await client.PutAsJsonAsync(
            $"/locations/{locationId}/images/{created!.ImageId}",
            new LocationImage {
                ImageId = created.ImageId,
                LocationId = locationId,
                ImageUrl = "/images/new.png",
                DisplayOrder = 2,
                Caption = "New"
            });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync($"/locations/{locationId}/images");
        var images = await get.Content.ReadFromJsonAsync<List<LocationImage>>();
        var loaded = Assert.Single(images!);
        Assert.Equal("/images/new.png", loaded.ImageUrl);
        Assert.Equal("New", loaded.Caption);
    }

    [Fact]
    public async Task DeleteRemovesImage() {
        var locationId = await CreateLocationAsync();
        var post = await client.PostAsJsonAsync($"/locations/{locationId}/images", new LocationImage {
            ImageUrl = "/images/doomed.png"
        });
        var created = await post.Content.ReadFromJsonAsync<LocationImage>();

        var del = await client.DeleteAsync($"/locations/{locationId}/images/{created!.ImageId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/locations/{locationId}/images");
        var images = await get.Content.ReadFromJsonAsync<List<LocationImage>>();
        Assert.Empty(images!);
    }
}
