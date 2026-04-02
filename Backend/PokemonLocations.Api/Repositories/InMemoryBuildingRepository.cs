using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class InMemoryBuildingRepository : IBuildingRepository {
    private readonly List<Building> buildings = new();
    private int nextBuildingId = 1;
    private int nextGymId = 1;

    public Task<IEnumerable<Building>> GetAllByLocationAsync(int locationId) {
        var result = buildings.Where(b => b.LocationId == locationId);
        return Task.FromResult<IEnumerable<Building>>(result);
    }

    public Task<Building?> GetByIdAsync(int buildingId) {
        var building = buildings.FirstOrDefault(b => b.BuildingId == buildingId);
        return Task.FromResult(building);
    }

    public Task<int> CreateAsync(Building building) {
        building.BuildingId = nextBuildingId++;
        if (building.Gym != null) {
            building.Gym.GymId = nextGymId++;
            building.Gym.BuildingId = building.BuildingId;
        }
        buildings.Add(building);
        return Task.FromResult(building.BuildingId);
    }

    public Task<bool> UpdateAsync(Building building) {
        var index = buildings.FindIndex(b => b.BuildingId == building.BuildingId);
        if (index == -1) return Task.FromResult(false);
        buildings[index] = building;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int buildingId) {
        var removed = buildings.RemoveAll(b => b.BuildingId == buildingId);
        return Task.FromResult(removed > 0);
    }
}
