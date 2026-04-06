using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class InMemoryGymRepository : IGymRepository {
    private readonly List<GymSummary> gyms = new() {
        new() { GymId = 1, BuildingId = 1, LocationId = 2, LocationName = "Pewter City", BuildingName = "Pewter City Gym", GymType = "Rock", BadgeName = "Boulder Badge", GymLeader = "Brock", GymOrder = 1 },
        new() { GymId = 2, BuildingId = 2, LocationId = 3, LocationName = "Cerulean City", BuildingName = "Cerulean City Gym", GymType = "Water", BadgeName = "Cascade Badge", GymLeader = "Misty", GymOrder = 2 },
        new() { GymId = 3, BuildingId = 3, LocationId = 4, LocationName = "Vermilion City", BuildingName = "Vermilion City Gym", GymType = "Electric", BadgeName = "Thunder Badge", GymLeader = "Lt. Surge", GymOrder = 3 },
        new() { GymId = 4, BuildingId = 4, LocationId = 5, LocationName = "Celadon City", BuildingName = "Celadon City Gym", GymType = "Grass", BadgeName = "Rainbow Badge", GymLeader = "Erika", GymOrder = 4 },
        new() { GymId = 5, BuildingId = 5, LocationId = 6, LocationName = "Fuchsia City", BuildingName = "Fuchsia City Gym", GymType = "Poison", BadgeName = "Soul Badge", GymLeader = "Koga", GymOrder = 5 },
        new() { GymId = 6, BuildingId = 6, LocationId = 7, LocationName = "Saffron City", BuildingName = "Saffron City Gym", GymType = "Psychic", BadgeName = "Marsh Badge", GymLeader = "Sabrina", GymOrder = 6 },
        new() { GymId = 7, BuildingId = 7, LocationId = 8, LocationName = "Cinnabar Island", BuildingName = "Cinnabar Island Gym", GymType = "Fire", BadgeName = "Volcano Badge", GymLeader = "Blaine", GymOrder = 7 },
        new() { GymId = 8, BuildingId = 8, LocationId = 9, LocationName = "Viridian City", BuildingName = "Viridian City Gym", GymType = "Ground", BadgeName = "Earth Badge", GymLeader = "Giovanni", GymOrder = 8 }
    };

    public Task<IEnumerable<GymSummary>> GetAllAsync() {
        return Task.FromResult<IEnumerable<GymSummary>>(gyms.OrderBy(g => g.GymOrder));
    }

    public Task<GymSummary?> GetByIdAsync(int gymId) {
        var gym = gyms.FirstOrDefault(g => g.GymId == gymId);
        return Task.FromResult(gym);
    }
}
