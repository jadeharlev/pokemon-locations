using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public class PokemonLocationsWebServerFactory : WebApplicationFactory<Program> {
    public const string JwtKey = "pokemon-locations-webserver-test-key-must-be-32-bytes-or-more!";
    public const string JwtIssuer = "pokemon-locations-api-test";
    public const string JwtAudience = "pokemon-locations-clients-test";

    private readonly string postgresConnectionString;
    private readonly string redisConnectionString;

    public TestOverridesAccessor Overrides { get; } = new();

    public string UploadRoot { get; } = Path.Combine(
        Path.GetTempPath(),
        "pokemon-locations-tests",
        Guid.NewGuid().ToString());

    public PokemonLocationsWebServerFactory(string postgresConnectionString, string redisConnectionString) {
        this.postgresConnectionString = postgresConnectionString;
        this.redisConnectionString = redisConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", postgresConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", redisConnectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("PokemonLocationsApi:BaseUrl", "http://api.test");
        builder.UseSetting("StarTrekWeatherApi:BaseUrl", "http://weather.test");
        builder.UseSetting("StarTrekWeatherApi:Username", "test");
        builder.UseSetting("StarTrekWeatherApi:Password", "test");
        builder.UseSetting("UserImages:UploadRoot", UploadRoot);
        builder.ConfigureTestServices(services => {
            services.AddSingleton(Overrides);
            OverrideWithFallback<IPokemonLocationsApiClient>(services, a => a.ApiClient);
            OverrideWithFallback<IStarTrekWeatherApiClient>(services, a => a.WeatherClient);
            OverrideWithFallback<IUserImageRepository>(services, a => a.UserImageRepository);
        });
    }

    private static void OverrideWithFallback<TService>(
        IServiceCollection services,
        Func<TestServiceOverrides, TService?> getOverride) where TService : class {
        var originals = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var d in originals) services.Remove(d);
        var original = originals.LastOrDefault();

        TService? cachedFallback = null;
        object cacheLock = new();
        TService ResolveFallback(IServiceProvider sp) {
            if (original is null) {
                throw new InvalidOperationException(
                    $"No registration for {typeof(TService).Name} and no override provided.");
            }
            if (original.Lifetime == ServiceLifetime.Singleton) {
                if (cachedFallback is not null) return cachedFallback;
                lock (cacheLock) {
                    cachedFallback ??= BuildFromDescriptor<TService>(sp, original);
                    return cachedFallback;
                }
            }
            return BuildFromDescriptor<TService>(sp, original);
        }

        services.AddTransient<TService>(sp => {
            var accessor = sp.GetRequiredService<TestOverridesAccessor>();
            if (accessor.Current is { } o && getOverride(o) is { } overridden) {
                return overridden;
            }
            return ResolveFallback(sp);
        });
    }

    private static TService BuildFromDescriptor<TService>(IServiceProvider sp, ServiceDescriptor d)
        where TService : class {
        if (d.ImplementationFactory is { } factory) return (TService)factory(sp);
        if (d.ImplementationInstance is TService instance) return instance;
        if (d.ImplementationType is { } implType) {
            return (TService)ActivatorUtilities.CreateInstance(sp, implType);
        }
        throw new InvalidOperationException($"Unsupported descriptor for {typeof(TService).Name}");
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(UploadRoot)) {
            try { Directory.Delete(UploadRoot, recursive: true); } catch { /* swallow */ }
        }
    }
}
