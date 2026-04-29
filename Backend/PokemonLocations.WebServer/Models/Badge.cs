namespace PokemonLocations.WebServer.Models;

public enum Badge {
    Boulder,
    Cascade,
    Thunder,
    Rainbow,
    Soul,
    Marsh,
    Volcano,
    Earth
}

public static class BadgeParser {
    public static bool TryParse(string? value, out Badge badge) {
        badge = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (var name in Enum.GetNames<Badge>()) {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) {
                badge = Enum.Parse<Badge>(name);
                return true;
            }
        }
        return false;
    }

    public static string ToWireFormat(Badge badge) => badge.ToString().ToLowerInvariant();
}
