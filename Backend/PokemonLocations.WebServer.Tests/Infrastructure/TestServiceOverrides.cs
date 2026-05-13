using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public sealed class TestServiceOverrides {
    public IPokemonLocationsApiClient? ApiClient { get; set; }
    public IStarTrekWeatherApiClient? WeatherClient { get; set; }
    public IUserImageRepository? UserImageRepository { get; set; }
}

public sealed class TestOverridesAccessor {
    public TestServiceOverrides? Current { get; set; }
}
