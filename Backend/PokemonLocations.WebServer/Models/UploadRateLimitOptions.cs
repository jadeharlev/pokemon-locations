namespace PokemonLocations.WebServer.Models;

public class UploadRateLimitOptions {
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
