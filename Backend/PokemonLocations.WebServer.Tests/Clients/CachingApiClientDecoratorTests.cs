using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using NSubstitute;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Tests.Infrastructure;

namespace PokemonLocations.WebServer.Tests.Clients;

[Collection("Redis")]
public class CachingApiClientDecoratorTests {
    private readonly RedisFixture redis;

    public CachingApiClientDecoratorTests(RedisFixture redis) {
        this.redis = redis;
    }

    private RedisCache CreateCache() => new(Options.Create(new RedisCacheOptions {
        Configuration = redis.ConnectionString,
        InstanceName = Guid.NewGuid().ToString()
    }));

    [Fact]
    public async Task GetAsyncFirstCallHitsInnerClient() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetAsync("/locations").Returns("[\"Pallet Town\"]");
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        var body = await decorator.GetAsync("/locations");

        Assert.Equal("[\"Pallet Town\"]", body);
        await inner.Received(1).GetAsync("/locations");
    }

    [Fact]
    public async Task GetAsyncSecondCallWithinTtlHitsCacheNotInner() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetAsync("/locations").Returns("[\"Pallet Town\"]");
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        await decorator.GetAsync("/locations");
        var second = await decorator.GetAsync("/locations");

        Assert.Equal("[\"Pallet Town\"]", second);
        await inner.Received(1).GetAsync("/locations");
    }

    [Fact]
    public async Task ExistsAsyncFirstCallHitsInner() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.ExistsAsync("/locations/1").Returns(true);
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        Assert.True(await decorator.ExistsAsync("/locations/1"));
        await inner.Received(1).ExistsAsync("/locations/1");
    }

    [Fact]
    public async Task ExistsAsyncSecondCallHitsCache() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.ExistsAsync("/locations/1").Returns(true);
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        await decorator.ExistsAsync("/locations/1");
        var second = await decorator.ExistsAsync("/locations/1");

        Assert.True(second);
        await inner.Received(1).ExistsAsync("/locations/1");
    }

    [Fact]
    public async Task ExistsAsyncCachesNegativeResult() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.ExistsAsync("/locations/999").Returns(false);
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        await decorator.ExistsAsync("/locations/999");
        var second = await decorator.ExistsAsync("/locations/999");

        Assert.False(second);
        await inner.Received(1).ExistsAsync("/locations/999");
    }

    [Fact]
    public async Task ExistsAsyncDoesNotCollideWithGetAsync() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetAsync("/locations/1").Returns("[\"Pallet Town\"]");
        inner.ExistsAsync("/locations/1").Returns(true);
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        var body = await decorator.GetAsync("/locations/1");
        var exists = await decorator.ExistsAsync("/locations/1");

        Assert.Equal("[\"Pallet Town\"]", body);
        Assert.True(exists);
        await inner.Received(1).GetAsync("/locations/1");
        await inner.Received(1).ExistsAsync("/locations/1");
    }

    [Fact]
    public async Task GetAsyncDifferentPathsDoNotCollide() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetAsync("/locations").Returns("[\"Pallet Town\"]");
        inner.GetAsync("/buildings").Returns("[\"Oak Lab\"]");
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        var locations = await decorator.GetAsync("/locations");
        var buildings = await decorator.GetAsync("/buildings");

        Assert.Equal("[\"Pallet Town\"]", locations);
        Assert.Equal("[\"Oak Lab\"]", buildings);
    }

    [Fact]
    public async Task GetWithStatusAsyncCacheMissCallsInner() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetWithStatusAsync("/locations").Returns(new ApiResponse(200, "[\"Pallet Town\"]"));
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        var result = await decorator.GetWithStatusAsync("/locations");

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("[\"Pallet Town\"]", result.Body);
        await inner.Received(1).GetWithStatusAsync("/locations");
    }

    [Fact]
    public async Task GetWithStatusAsyncSecondCallHitsCache() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetWithStatusAsync("/locations").Returns(new ApiResponse(200, "[\"Pallet Town\"]"));
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        await decorator.GetWithStatusAsync("/locations");
        var second = await decorator.GetWithStatusAsync("/locations");

        Assert.Equal(200, second.StatusCode);
        Assert.Equal("[\"Pallet Town\"]", second.Body);
        await inner.Received(1).GetWithStatusAsync("/locations");
    }

    [Fact]
    public async Task GetWithStatusAsyncDoesNotCacheNon200() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetWithStatusAsync("/locations/999").Returns(new ApiResponse(404, null));
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        await decorator.GetWithStatusAsync("/locations/999");
        await decorator.GetWithStatusAsync("/locations/999");

        await inner.Received(2).GetWithStatusAsync("/locations/999");
    }

    [Fact]
    public async Task GetAsyncAndGetWithStatusAsyncShareCache() {
        var inner = Substitute.For<IPokemonLocationsApiClient>();
        inner.GetAsync("/locations").Returns("[\"Pallet Town\"]");
        inner.GetWithStatusAsync("/locations").Returns(new ApiResponse(200, "[\"Pallet Town\"]"));
        var decorator = new CachingApiClientDecorator(inner, CreateCache(), TimeSpan.FromMinutes(5));

        // Populate cache via GetAsync
        await decorator.GetAsync("/locations");

        // GetWithStatusAsync should hit the shared cache, not the inner client
        var result = await decorator.GetWithStatusAsync("/locations");

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("[\"Pallet Town\"]", result.Body);
        await inner.Received(1).GetAsync("/locations");
        await inner.DidNotReceive().GetWithStatusAsync(Arg.Any<string>());
    }
}
