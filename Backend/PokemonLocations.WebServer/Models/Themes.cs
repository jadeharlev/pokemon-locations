namespace PokemonLocations.WebServer.Models;

public static class Themes {
    public const string Bulbasaur = "bulbasaur";
    public const string Charmander = "charmander";
    public const string Squirtle = "squirtle";
    public const string Pikachu = "pikachu";

    private static readonly HashSet<string> All = new(StringComparer.Ordinal) {
        Bulbasaur, Charmander, Squirtle, Pikachu
    };

    public static bool IsValid(string? theme) => theme is not null && All.Contains(theme);
}
