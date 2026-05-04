using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public interface ILocationRepository {
    Task<IEnumerable<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(int locationId);
}
