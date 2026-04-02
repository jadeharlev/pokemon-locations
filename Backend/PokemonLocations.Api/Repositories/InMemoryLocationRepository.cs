using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class InMemoryLocationRepository : ILocationRepository {
    private readonly List<Location> locations = new();
    private int nextId = 1;

    public Task<IEnumerable<Location>> GetAllAsync() {
        return Task.FromResult<IEnumerable<Location>>(locations);
    }

    public Task<Location?> GetByIdAsync(int locationId) {
        var location = locations.FirstOrDefault(l => l.LocationId == locationId);
        return Task.FromResult(location);
    }

    public Task<int> CreateAsync(Location location) {
        location.LocationId = nextId++;
        locations.Add(location);
        return Task.FromResult(location.LocationId);
    }

    public Task<bool> UpdateAsync(Location location) {
        var index = locations.FindIndex(l => l.LocationId == location.LocationId);
        if (index == -1) return Task.FromResult(false);
        locations[index] = location;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int locationId) {
        var removed = locations.RemoveAll(l => l.LocationId == locationId);
        return Task.FromResult(removed > 0);
    }
}
