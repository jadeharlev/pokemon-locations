using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class InMemoryGymRepository : IGymRepository {
    private readonly List<GymSummary> gyms = new() {
        new() { GymId = 2, BuildingId = 10, LocationId = 3, LocationName = "Pewter City", BuildingName = "Pewter City Gym", GymType = "Rock", BadgeName = "Boulder Badge", GymLeader = "Brock", GymOrder = 1 },
        new() { GymId = 3, BuildingId = 14, LocationId = 4, LocationName = "Cerulean City", BuildingName = "Cerulean City Gym", GymType = "Water", BadgeName = "Cascade Badge", GymLeader = "Misty", GymOrder = 2 },
        new() { GymId = 1, BuildingId = 6, LocationId = 2, LocationName = "Viridian City", BuildingName = "Viridian City Gym", GymType = "Ground", BadgeName = "Earth Badge", GymLeader = "Giovanni", GymOrder = 8 },
    };

    public Task<IEnumerable<GymSummary>> GetAllAsync() {
        return Task.FromResult<IEnumerable<GymSummary>>(gyms.OrderBy(g => g.GymOrder));
    }

    public Task<GymSummary?> GetByIdAsync(int gymId) {
        var gym = gyms.FirstOrDefault(g => g.GymId == gymId);
        return Task.FromResult(gym);
    }
}
