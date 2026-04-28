using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Npgsql.NameTranslation;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Database;
using PokemonLocations.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

#region Services
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is missing");

var migrationLoggerFactory = LoggerFactory.Create(logging => {
    logging.AddConsole();
});

var migrationLogger = migrationLoggerFactory.CreateLogger("MigrationRunner");

var migrationResult = MigrationRunner.Run(postgresConnectionString, migrationLogger);

if (!migrationResult.Successful) {
    throw new InvalidOperationException("Database migration failed", migrationResult.Error);
}

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
dataSourceBuilder.MapEnum<BuildingType>(
    pgName: "building_type",
    nameTranslator: new NpgsqlSnakeCaseNameTranslator());
var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);

builder.Services.AddScoped<IDatabaseHealthRepository, DatabaseHealthRepository>();
builder.Services.AddScoped<ILocationRepository, DapperLocationRepository>();
builder.Services.AddScoped<IBuildingRepository, DapperBuildingRepository>();
builder.Services.AddScoped<IGymRepository, DapperGymRepository>();
builder.Services.AddScoped<ILocationImageRepository, DapperLocationImageRepository>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
#endregion


var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");

app.MapControllers();
app.Run();

public partial class Program { }
