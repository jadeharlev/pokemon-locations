using Testcontainers.Redis;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class RedisFixture : IAsyncLifetime {
    private static readonly Lazy<Task<RedisContainer>> sharedContainer = new(StartSharedContainerAsync);
    private static int nextDatabase = -1;

    private string connectionString = null!;

    public string ConnectionString => connectionString;

    public async Task InitializeAsync() {
        connectionString = await AllocateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    internal static async Task<string> AllocateAsync() {
        var container = await sharedContainer.Value;
        var db = Interlocked.Increment(ref nextDatabase);
        return $"{container.GetConnectionString()},defaultDatabase={db}";
    }

    private static async Task<RedisContainer> StartSharedContainerAsync() {
        var builder = new RedisBuilder()
            .WithImage("redis:8-alpine")
            .Build();
        await builder.StartAsync();
        return builder;
    }
}

[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisFixture> { }
