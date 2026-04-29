using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class PokemonLocationsWebServerFactory : WebApplicationFactory<Program> {
    public const string JwtKey = "pokemon-locations-webserver-test-key-must-be-32-bytes-or-more!";
    public const string JwtIssuer = "pokemon-locations-api-test";
    public const string JwtAudience = "pokemon-locations-clients-test";

    public IPokemonLocationsApiClient? ApiClient { get; init; }

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
        if (ApiClient is not null) {
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IPokemonLocationsApiClient>();
                services.AddSingleton(ApiClient);
            });
        }
    }
}
