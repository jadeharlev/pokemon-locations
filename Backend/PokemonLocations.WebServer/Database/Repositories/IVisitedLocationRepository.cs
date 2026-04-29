namespace PokemonLocations.WebServer.Database.Repositories;

public interface IVisitedLocationRepository {
    Task<IReadOnlyList<int>> GetForUserAsync(int userId);
    Task AddAsync(int userId, int locationId);
    Task RemoveAsync(int userId, int locationId);
}
