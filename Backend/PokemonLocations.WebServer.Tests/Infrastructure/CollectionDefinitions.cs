namespace PokemonLocations.WebServer.Tests.Infrastructure;

[CollectionDefinition("PostgresAndRedis")]
public class PostgresAndRedisCollection
    : ICollectionFixture<PostgresFixture>, ICollectionFixture<RedisFixture> { }
