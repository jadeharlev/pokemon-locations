using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PokemonLocations.Api.Tests.Infrastructure;

public class PokemonLocationsApiFactory : WebApplicationFactory<Program> {
    public PokemonLocationsApiFactory(string connectionString) {
        // Program.cs reads ConnectionStrings:Postgres before builder.Build(),
        // so ConfigureAppConfiguration hooks applied by WebApplicationFactory run too late.
        // Setting the environmental variable before the factory boots lets
        // WebApplication.CreateBuilder pick it up.
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
    }
}
