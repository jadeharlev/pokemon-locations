namespace PokemonLocations.WebServer.Models;

public class User {
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Theme { get; set; } = "bulbasaur";
    public DateTime CreatedAt { get; set; }
}
