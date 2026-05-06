namespace PokemonLocations.WebServer.Models.Responses;

public record MeResponse(
    int UserId,
    string Email,
    string DisplayName,
    string Theme,
    string? PermanentPlanetName) {
    public static MeResponse FromUser(Models.User user) =>
        new(user.UserId, user.Email, user.DisplayName, user.Theme, user.PermanentPlanetName);
}
