namespace PokemonLocations.WebServer.Database.Repositories;

public interface IVisitedBuildingRepository {
    Task<IReadOnlyList<int>> GetForUserAsync(int userId);
    Task AddAsync(int userId, int buildingId);
    Task RemoveAsync(int userId, int buildingId);
}
