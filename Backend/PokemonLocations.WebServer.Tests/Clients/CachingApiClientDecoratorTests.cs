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
}
