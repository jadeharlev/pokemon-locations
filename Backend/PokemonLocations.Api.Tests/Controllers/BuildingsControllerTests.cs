using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PokemonLocations.Api.Controllers;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Tests.Controllers;

public class BuildingsControllerTests {
    private static BuildingsController CreateController(
        IBuildingRepository buildingRepo,
        ILocationRepository locationRepo) {
        return new BuildingsController(
            buildingRepo,
            locationRepo,
            NullLogger<BuildingsController>.Instance);
    }

    [Fact]
    public async Task GetAllBuildingsReturnsOkWithListOfBuildings() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        buildingRepo.GetAllByLocationAsync(1).Returns(new List<Building>());
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.GetAll(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<List<Building>>(okResult.Value);
    }

    [Fact]
    public async Task GetAllBuildingsReturnsNotFoundWhenLocationDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.GetAll(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByIdReturnsOkWithBuildingWhenBuildingExists() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pewter City" });
        var expected = new Building { BuildingId = 4, LocationId = 1, Name = "Pewter City Gym", BuildingType = BuildingType.Gym };
        buildingRepo.GetByIdAsync(4).Returns(expected);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.GetById(1, 4);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var building = Assert.IsType<Building>(okResult.Value);
        Assert.Equal(expected, building);
    }

    [Fact]
    public async Task GetByIdReturnsOkWithGymDetailsWhenBuildingIsGym() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pewter City" });

        var expected = new Building {
            BuildingId = 4,
            LocationId = 1,
            Name = "Pewter City Gym",
            BuildingType = BuildingType.Gym,
            Gym = new Gym {
                GymId = 1,
                BuildingId = 4,
                GymType = "Rock",
                BadgeName = "Boulder Badge",
                GymLeader = "Brock",
                GymOrder = 1
            }
        };

        buildingRepo.GetByIdAsync(4).Returns(expected);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.GetById(1, 4);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var building = Assert.IsType<Building>(okResult.Value);
        Assert.NotNull(building.Gym);
        Assert.Equal("Rock", building.Gym.GymType);
        Assert.Equal("Brock", building.Gym.GymLeader);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundWhenLocationDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.GetById(1, 4);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundWhenBuildingDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pewter City" });
        buildingRepo.GetByIdAsync(4).Returns((Building?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.GetById(1, 4);

        Assert.IsType<NotFoundResult>(result);
    }
}
