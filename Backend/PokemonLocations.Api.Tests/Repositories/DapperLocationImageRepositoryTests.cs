using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Repositories;

[Collection("Postgres")]
public class DapperLocationImageRepositoryTests : IAsyncLifetime {
    private readonly PostgresFixture postgres;
    private NpgsqlDataSource dataSource = null!;

    public DapperLocationImageRepositoryTests(PostgresFixture postgres) {
        this.postgres = postgres;
    }

    public async Task InitializeAsync() {
        dataSource = new NpgsqlDataSourceBuilder(postgres.ConnectionString).Build();
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE TABLE locations RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() {
        dataSource?.Dispose();
        return Task.CompletedTask;
    }

    private DapperLocationImageRepository CreateNewRepository() {
        return new DapperLocationImageRepository(
            dataSource,
            NullLogger<DapperLocationImageRepository>.Instance);
    }

    private async Task<int> SeedLocationAsync(string name = "Test Town") {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO locations (name) VALUES (@Name) RETURNING location_id",
            new { Name = name });
    }

    private static LocationImage NewImage(
        int locationId,
        string imageUrl = "/images/test.png",
        int displayOrder = 0,
        string? caption = "A caption") => new() {
            LocationId = locationId,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder,
            Caption = caption
        };

    [Fact]
    public async Task GetAllByLocationAsyncReturnsEmptyWhenNoImagesExist() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var images = await repository.GetAllByLocationAsync(locationId);

        Assert.Empty(images);
    }

    [Fact]
    public async Task GetAllByLocationAsyncReturnsOnlyImagesForGivenLocation() {
        var palletId = await SeedLocationAsync("Pallet Town");
        var pewterId = await SeedLocationAsync("Pewter City");
        var repository = CreateNewRepository();

        await repository.CreateAsync(NewImage(palletId, "/images/pallet-1.png", 1));
        await repository.CreateAsync(NewImage(palletId, "/images/pallet-2.png", 2));
        await repository.CreateAsync(NewImage(pewterId, "/images/pewter-1.png", 1));

        var palletImages = (await repository.GetAllByLocationAsync(palletId)).ToList();

        Assert.Equal(2, palletImages.Count);
        Assert.All(palletImages, i => Assert.Equal(palletId, i.LocationId));
    }

    [Fact]
    public async Task GetAllByLocationAsyncOrdersImagesByDisplayOrder() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        await repository.CreateAsync(NewImage(locationId, "/images/c.png", 3));
        await repository.CreateAsync(NewImage(locationId, "/images/a.png", 1));
        await repository.CreateAsync(NewImage(locationId, "/images/b.png", 2));

        var images = (await repository.GetAllByLocationAsync(locationId)).ToList();

        Assert.Equal(new[] { 1, 2, 3 }, images.Select(i => i.DisplayOrder));
        Assert.Equal(
            new[] { "/images/a.png", "/images/b.png", "/images/c.png" },
            images.Select(i => i.ImageUrl));
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenImageDoesNotExist() {
        var repository = CreateNewRepository();

        var loaded = await repository.GetByIdAsync(999_999);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsImageWhenItExists() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(
            NewImage(locationId, "/images/overview.png", 1, "Overview"));

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Equal(newId, loaded!.ImageId);
        Assert.Equal(locationId, loaded.LocationId);
        Assert.Equal("/images/overview.png", loaded.ImageUrl);
        Assert.Equal(1, loaded.DisplayOrder);
        Assert.Equal("Overview", loaded.Caption);
    }

    [Fact]
    public async Task CreateAsyncInsertsImageAndReturnsId() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(NewImage(locationId));

        Assert.True(newId > 0);

        await using var connection = await dataSource.OpenConnectionAsync();
        var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM location_images");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateAsyncAllowsNullCaption() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(NewImage(locationId, caption: null));

        var loaded = await repository.GetByIdAsync(newId);

        Assert.NotNull(loaded);
        Assert.Null(loaded!.Caption);
    }

    [Fact]
    public async Task UpdateAsyncMutatesExistingImageAndReturnsTrue() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(NewImage(locationId, "/images/old.png", 1, "Old"));

        var update = new LocationImage {
            ImageId = newId,
            LocationId = locationId,
            ImageUrl = "/images/new.png",
            DisplayOrder = 5,
            Caption = "New"
        };

        var result = await repository.UpdateAsync(update);

        Assert.True(result);

        var loaded = await repository.GetByIdAsync(newId);
        Assert.Equal("/images/new.png", loaded!.ImageUrl);
        Assert.Equal(5, loaded.DisplayOrder);
        Assert.Equal("New", loaded.Caption);
    }

    [Fact]
    public async Task UpdateAsyncReturnsFalseWhenImageDoesNotExist() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var result = await repository.UpdateAsync(new LocationImage {
            ImageId = 999_999,
            LocationId = locationId,
            ImageUrl = "/images/ghost.png"
        });

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsyncRemovesImageAndReturnsTrue() {
        var locationId = await SeedLocationAsync();
        var repository = CreateNewRepository();

        var newId = await repository.CreateAsync(NewImage(locationId));

        var result = await repository.DeleteAsync(newId);

        Assert.True(result);
        Assert.Null(await repository.GetByIdAsync(newId));
    }

    [Fact]
    public async Task DeleteAsyncReturnsFalseWhenImageDoesNotExist() {
        var repository = CreateNewRepository();

        var result = await repository.DeleteAsync(999_999);

        Assert.False(result);
    }
}
