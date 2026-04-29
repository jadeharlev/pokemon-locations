namespace PokemonLocations.WebServer.Authentication;

public static class EmailNormalizer {
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
