namespace PokemonLocations.WebServer.Models;

public static class Themes
{
    public const string Bulbasaur = "bulbasaur";
    public const string Charmander = "charmander";
    public const string Squirtle = "squirtle";
    public const string Pikachu = "pikachu";

    public const string Rattata = "rattata";
    public const string Diglett = "diglett";
    public const string Geodude = "geodude";
    public const string Dratini = "dratini";
    public const string Mew = "mew";
    public const string Dragonite = "dragonite";

    private static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        Bulbasaur,
        Charmander,
        Squirtle,
        Pikachu,
        Rattata,
        Diglett,
        Geodude,
        Dratini,
        Mew,
        Dragonite
    };

    public static bool IsValid(string? theme) => theme is not null && All.Contains(theme);
}
