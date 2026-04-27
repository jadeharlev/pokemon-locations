var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "PokemonLocations Web Server");

app.Run();

public partial class Program { }
