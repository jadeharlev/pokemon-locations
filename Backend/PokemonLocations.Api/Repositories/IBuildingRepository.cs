using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public interface IBuildingRepository {
    Task<IEnumerable<Building>> GetAllByLocationAsync(int locationId);
    Task<Building?> GetByIdAsync(int buildingId);
}
