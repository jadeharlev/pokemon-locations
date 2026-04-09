using Npgsql;
using Npgsql.NameTranslation;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Database;
using PokemonLocations.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

#region Services
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is missing");

var migrationResult = MigrationRunner.Run(postgresConnectionString);
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

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");

app.MapControllers();
app.Run();

public partial class Program { }
