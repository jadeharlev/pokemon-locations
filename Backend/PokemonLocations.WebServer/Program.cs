using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Services;
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
builder.Services.AddSingleton<IVisitedBuildingRepository, VisitedBuildingRepository>();
builder.Services.AddSingleton<IUserNoteRepository, UserNoteRepository>();
builder.Services.AddSingleton<IUserImageRepository, UserImageRepository>();
builder.Services.AddSingleton<IImageProcessor, ImageProcessor>();
builder.Services.Configure<UserImagesOptions>(
    builder.Configuration.GetSection("UserImages"));

builder.Services.Configure<FormOptions>(opts => {
    opts.MultipartBodyLengthLimit = 12_582_912;
    opts.MultipartHeadersLengthLimit = 32_768;
});
builder.WebHost.ConfigureKestrel(opts => {
    opts.Limits.MaxRequestBodySize = 12_582_912;
});

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

var weatherBaseUrl = builder.Configuration["StarTrekWeatherApi:BaseUrl"]
    ?? throw new InvalidOperationException("StarTrekWeatherApi:BaseUrl is missing");
var weatherUser = builder.Configuration["StarTrekWeatherApi:Username"]
    ?? throw new InvalidOperationException("StarTrekWeatherApi:Username is missing");
var weatherPass = builder.Configuration["StarTrekWeatherApi:Password"]
    ?? throw new InvalidOperationException("StarTrekWeatherApi:Password is missing");
var weatherCreds = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{weatherUser}:{weatherPass}"));
builder.Services.AddHttpClient<IStarTrekWeatherApiClient, StarTrekWeatherApiClient>(client => {
    client.BaseAddress = new Uri(weatherBaseUrl);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", weatherCreds);
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
        options.SuppressWWWAuthenticateHeader = true;
    });

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<UploadRateLimitOptions>(
    builder.Configuration.GetSection("RateLimits:Upload"));
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("upload", httpContext => {
        var key = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.GetUserId().ToString()
            : "anonymous";
        var settings = httpContext.RequestServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<UploadRateLimitOptions>>()
            .CurrentValue;
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddControllers();
#endregion

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions {
    OnPrepareResponse = ctx => {
        if (app.Environment.IsDevelopment()) {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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
