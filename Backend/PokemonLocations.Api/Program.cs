using System.Data;
using Npgsql;
using PokemonLocations.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddScoped<IDatabaseHealthRepository, DatabaseHealthRepository>();
builder.Services.AddSingleton<ILocationRepository, InMemoryLocationRepository>();
builder.Services.AddSingleton<IBuildingRepository, InMemoryBuildingRepository>();

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

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddScoped<IDbConnection>(serviceProvider => new NpgsqlConnection(postgresConnectionString));
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