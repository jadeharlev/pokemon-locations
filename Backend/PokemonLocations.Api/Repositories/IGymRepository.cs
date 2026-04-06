using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public interface IGymRepository {
    Task<IEnumerable<GymSummary>> GetAllAsync();
    Task<GymSummary?> GetByIdAsync(int gymId);
}
