using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class InMemoryBuildingRepository : IBuildingRepository {
    private readonly List<Building> buildings;
    private int nextBuildingId;
    private int nextGymId;

    public InMemoryBuildingRepository() {
        buildings = new List<Building> {
            // === Pallet Town (LocationId: 1) ===
            new() { BuildingId = 1, LocationId = 1, Name = "Player's House", BuildingType = BuildingType.Residential, Description = "The home of the player character in Pallet Town." },
            new() { BuildingId = 2, LocationId = 1, Name = "Rival's House", BuildingType = BuildingType.Residential, Description = "The home of the player's rival and Professor Oak's grandson." },
            new() { BuildingId = 3, LocationId = 1, Name = "Professor Oak's Lab", BuildingType = BuildingType.Lab, Description = "The Pokemon research laboratory where trainers receive their first Pokemon." },

            // === Viridian City (LocationId: 2) ===
            new() { BuildingId = 4, LocationId = 2, Name = "Viridian City Pokemon Center", BuildingType = BuildingType.PokemonCenter, Description = "A Pokemon Center where trainers can heal their Pokemon." },
            new() { BuildingId = 5, LocationId = 2, Name = "Viridian City Poke Mart", BuildingType = BuildingType.PokeMart, Description = "A shop that sells items useful for Pokemon trainers." },
            new() { BuildingId = 6, LocationId = 2, Name = "Viridian City Gym", BuildingType = BuildingType.Gym, Description = "The final gym in the Kanto region, led by Giovanni.",
                Gym = new() { GymId = 1, BuildingId = 6, GymType = "Ground", BadgeName = "Earth Badge", GymLeader = "Giovanni", GymOrder = 8 } },
            new() { BuildingId = 7, LocationId = 2, Name = "Viridian City Trainer School", BuildingType = BuildingType.Landmark, Description = "A school that teaches the basics of Pokemon training.", LandmarkDescription = "A small building where new trainers learn the fundamentals of Pokemon battles and status conditions." },

            // === Pewter City (LocationId: 3) ===
            new() { BuildingId = 8, LocationId = 3, Name = "Pewter City Pokemon Center", BuildingType = BuildingType.PokemonCenter, Description = "A Pokemon Center where trainers can heal their Pokemon." },
            new() { BuildingId = 9, LocationId = 3, Name = "Pewter City Poke Mart", BuildingType = BuildingType.PokeMart, Description = "A shop that sells items useful for Pokemon trainers." },
            new() { BuildingId = 10, LocationId = 3, Name = "Pewter City Gym", BuildingType = BuildingType.Gym, Description = "The first gym in the Kanto region, led by Brock.",
                Gym = new() { GymId = 2, BuildingId = 10, GymType = "Rock", BadgeName = "Boulder Badge", GymLeader = "Brock", GymOrder = 1 } },
            new() { BuildingId = 11, LocationId = 3, Name = "Pewter Museum of Science", BuildingType = BuildingType.Landmark, Description = "A museum showcasing fossils and space exhibits.", LandmarkDescription = "A two-story museum featuring prehistoric Pokemon fossils on the first floor and a space shuttle exhibit on the second floor." },

            // === Cerulean City (LocationId: 4) ===
            new() { BuildingId = 12, LocationId = 4, Name = "Cerulean City Pokemon Center", BuildingType = BuildingType.PokemonCenter, Description = "A Pokemon Center where trainers can heal their Pokemon." },
            new() { BuildingId = 13, LocationId = 4, Name = "Cerulean City Poke Mart", BuildingType = BuildingType.PokeMart, Description = "A shop that sells items useful for Pokemon trainers." },
            new() { BuildingId = 14, LocationId = 4, Name = "Cerulean City Gym", BuildingType = BuildingType.Gym, Description = "The second gym in the Kanto region, led by Misty.",
                Gym = new() { GymId = 3, BuildingId = 14, GymType = "Water", BadgeName = "Cascade Badge", GymLeader = "Misty", GymOrder = 2 } },
            new() { BuildingId = 15, LocationId = 4, Name = "Bike Shop", BuildingType = BuildingType.Landmark, Description = "A shop that sells bicycles for Pokemon trainers.", LandmarkDescription = "A small bicycle shop offering a bike for the steep price of 1,000,000 Poke Dollars, or free with a Bike Voucher." },
        };

        nextBuildingId = buildings.Max(b => b.BuildingId) + 1;
        nextGymId = buildings.Where(b => b.Gym != null).Max(b => b.Gym!.GymId) + 1;
    }

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
