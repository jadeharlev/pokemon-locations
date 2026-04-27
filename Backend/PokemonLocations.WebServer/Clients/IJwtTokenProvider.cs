namespace PokemonLocations.WebServer.Clients;

public interface IJwtTokenProvider {
    string GetCurrentToken();
    void Refresh();
}
