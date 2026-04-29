using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database;
using PokemonLocations.WebServer.Database.Repositories;
using idunno.Authentication.Basic;

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
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IBadgeRepository, BadgeRepository>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<BasicAuthCredentialValidator>();

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
    .AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
    .AddBasic(options => {
        options.Realm = "PokemonLocations";
        options.AllowInsecureProtocol = builder.Environment.IsDevelopment();
        options.Events = new BasicAuthenticationEvents {
            OnValidateCredentials = async context => {
                var validator = context.HttpContext.RequestServices
                    .GetRequiredService<BasicAuthCredentialValidator>();
                var result = await validator.ValidateAsync(context.Username, context.Password);
                if (result.Success && result.Principal is not null) {
                    context.Principal = result.Principal;
                    context.Success();
                } else {
                    context.NoResult();
                }
            }
        };
    });

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();
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

app.MapControllers();

app.Run();

public partial class Program { }
