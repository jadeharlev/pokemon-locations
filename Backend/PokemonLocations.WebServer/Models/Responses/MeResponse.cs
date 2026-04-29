namespace PokemonLocations.WebServer.Models.Responses;

public record MeResponse(int UserId, string Email, string DisplayName, string Theme) {
    public static MeResponse FromUser(Models.User user) =>
        new(user.UserId, user.Email, user.DisplayName, user.Theme);
}
