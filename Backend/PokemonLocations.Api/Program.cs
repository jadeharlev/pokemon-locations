using System.Data;
using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddScoped<IDbConnection>(serviceProvider => new NpgsqlConnection(postgresConnectionString));
#endregion


var app = builder.Build();
    
app.UseHttpsRedirection();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");

app.MapGet("/health/db", async (IDbConnection db) =>
{
    db.Open();
    var result = await db.QuerySingleAsync<int>("SELECT 1");
    return Results.Ok("Database connected");
});

app.Run();