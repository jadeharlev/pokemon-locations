using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Database.Repositories;

public interface IUserRepository {
    Task<User> CreateAsync(string email, string passwordHash, string displayName);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int userId);
    Task DeleteAsync(int userId);
    Task UpdateThemeAsync(int userId, string theme);
    Task UpdatePermanentPlanetAsync(int userId, string? planetName);
    Task<(int MaxGymsComplete, int MaxLocationsVisited, int MaxBuildingsVisited)>
        BumpAndGetMaxStatsAsync(int userId, int gymsComplete, int locationsVisited, int buildingsVisited);
}
