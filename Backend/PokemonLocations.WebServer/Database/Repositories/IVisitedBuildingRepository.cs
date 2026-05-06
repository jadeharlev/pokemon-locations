namespace PokemonLocations.WebServer.Database.Repositories;

public interface IVisitedBuildingRepository
{
    Task<IReadOnlyList<int>> GetForUserAsync(int userId);

    Task<IReadOnlyList<int>> GetForUserLocationAsync(int userId, int locationId);

    Task AddAsync(int userId, int locationId, int buildingId);

    Task RemoveAsync(int userId, int locationId, int buildingId);

    Task<IReadOnlyList<int>> GetDistinctLocationIdsForUserAsync(int userId);
}
