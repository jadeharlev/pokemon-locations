using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public interface IBuildingRepository {
    Task<IEnumerable<Building>> GetAllByLocationAsync(int locationId);
    Task<Building?> GetByIdAsync(int buildingId);
    Task<int> CreateAsync(Building building);
    Task<bool> UpdateAsync(Building building);
    Task<bool> DeleteAsync(int buildingId);
}
