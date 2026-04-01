using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public interface ILocationRepository {
    Task<IEnumerable<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(int locationId);
    Task<int> CreateAsync(Location location);
    Task<bool> UpdateAsync(Location location);
    Task<bool> DeleteAsync(int locationId);
}
