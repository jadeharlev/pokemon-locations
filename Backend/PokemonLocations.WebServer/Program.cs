using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database;
using PokemonLocations.WebServer.Database.Repositories;

var builder = WebApplication.CreateBuilder(args);

#region Services
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is missing");
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string is missing");

var migrationResult = MigrationRunner.Run(postgresConnectionString);
if (!migrationResult.Successful) {
    throw new InvalidOperationException("Database migration failed", migrationResult.Error);
}

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var dataSource = NpgsqlDataSource.Create(postgresConnectionString);
builder.Services.AddSingleton(dataSource);
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<PasswordHasher>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services.AddSingleton<IJwtTokenProvider>(
    new JwtTokenProvider(jwtKey, jwtIssuer, jwtAudience));

var apiBaseUrl = builder.Configuration["PokemonLocationsApi:BaseUrl"]
    ?? throw new InvalidOperationException("PokemonLocationsApi:BaseUrl is missing");
builder.Services.AddHttpClient<PokemonLocationsApiClient>(client => {
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddSingleton<IPokemonLocationsApiClient>(provider => {
    var inner = provider.GetRequiredService<PokemonLocationsApiClient>();
    var cache = provider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
    return new CachingApiClientDecorator(inner, cache, TimeSpan.FromMinutes(5));
});

builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = redisConnectionString;
    options.InstanceName = "PokemonLocations.WebServer:";
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.Name = "PokemonLocations.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context => {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context => {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
#endregion

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/db", [AllowAnonymous] async (NpgsqlDataSource source) => {
    await using var connection = await source.OpenConnectionAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT 1";
    await command.ExecuteScalarAsync();
    return Results.Ok(new { status = "ok" });
});

app.MapGet("/api/me", () => Results.Ok());

app.Run();

public partial class Program { }
