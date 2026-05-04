using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public interface ILocationImageRepository {
    Task<IEnumerable<LocationImage>> GetAllByLocationAsync(int locationId);
    Task<LocationImage?> GetByIdAsync(int imageId);
}
