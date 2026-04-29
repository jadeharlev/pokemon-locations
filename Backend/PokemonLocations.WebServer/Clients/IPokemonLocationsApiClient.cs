namespace PokemonLocations.WebServer.Clients;

public interface IPokemonLocationsApiClient {
    Task<string> GetAsync(string path);
    Task<ApiResponse> GetWithStatusAsync(string path);
    Task<bool> ExistsAsync(string path);
}
