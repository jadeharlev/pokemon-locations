using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class PokemonLocationsWebServerFactory : WebApplicationFactory<Program> {
    public const string JwtKey = "pokemon-locations-webserver-test-key-must-be-32-bytes-or-more!";
    public const string JwtIssuer = "pokemon-locations-api-test";
    public const string JwtAudience = "pokemon-locations-clients-test";

    public IPokemonLocationsApiClient? ApiClient { get; init; }
    public IStarTrekWeatherApiClient? WeatherClient { get; init; }
    public IUserImageRepository? UserImageRepositoryOverride { get; set; }

    public string UploadRoot { get; } = Path.Combine(
        Path.GetTempPath(),
        "pokemon-locations-tests",
        Guid.NewGuid().ToString());

    public PokemonLocationsWebServerFactory(string postgresConnectionString, string redisConnectionString) {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", postgresConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", redisConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("PokemonLocationsApi__BaseUrl", "http://api.test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["UserImages:UploadRoot"] = UploadRoot
            });
        });
        if (ApiClient is not null) {
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IPokemonLocationsApiClient>();
                services.AddSingleton(ApiClient);
            });
        }
        if (WeatherClient is not null) {
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IStarTrekWeatherApiClient>();
                services.AddSingleton(WeatherClient);
            });
        }
        builder.ConfigureTestServices(services => {
            if (UserImageRepositoryOverride is not null) {
                services.RemoveAll(typeof(IUserImageRepository));
                services.AddSingleton(UserImageRepositoryOverride);
            }
        });
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(UploadRoot)) {
            try { Directory.Delete(UploadRoot, recursive: true); } catch { /* swallow */ }
        }
    }
}
