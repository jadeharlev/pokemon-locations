using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PokemonLocations.Api.Tests.Infrastructure;

public class PokemonLocationsApiFactory : WebApplicationFactory<Program> {
    public PokemonLocationsApiFactory(string connectionString) {
        // Program.cs reads configuration before builder.Build(), so
        // ConfigureAppConfiguration hooks applied by WebApplicationFactory run too late.
        // Setting environment variables before the factory boots lets
        // WebApplication.CreateBuilder pick them up.
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", connectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", JwtTokenTestHelper.Key);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtTokenTestHelper.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtTokenTestHelper.Audience);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
    }

    public HttpClient CreateAuthenticatedClient(string subject = "test-client") {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenTestHelper.Create(subject));
        return client;
    }
}
