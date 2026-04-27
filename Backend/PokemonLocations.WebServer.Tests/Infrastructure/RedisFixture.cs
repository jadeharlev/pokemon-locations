using Testcontainers.Redis;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class RedisFixture : IAsyncLifetime {
    private readonly RedisContainer container = new RedisBuilder()
        .WithImage("redis:8-alpine")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}

[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisFixture> { }
