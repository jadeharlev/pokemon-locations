namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class WebServerFixture : IAsyncLifetime {
    public string PostgresConnectionString { get; private set; } = null!;
    public string RedisConnectionString { get; private set; } = null!;
    public PokemonLocationsWebServerFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync() {
        PostgresConnectionString = await PostgresFixture.AllocateMigratedDatabaseAsync();
        RedisConnectionString = await RedisFixture.AllocateAsync();
        Factory = new PokemonLocationsWebServerFactory(PostgresConnectionString, RedisConnectionString);
    }

    public Task DisposeAsync() {
        Factory?.Dispose();
        return Task.CompletedTask;
    }
}
