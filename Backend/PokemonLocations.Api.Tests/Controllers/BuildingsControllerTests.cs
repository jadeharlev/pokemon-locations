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

    [Fact]
    public async Task CreateReturnsCreatedAtActionWithNewBuilding() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });

        var newBuilding = new Building {
            Name = "Oak's Lab",
            BuildingType = BuildingType.Lab
        };

        buildingRepo.CreateAsync(newBuilding).Returns(1);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Create(1, newBuilding);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var building = Assert.IsType<Building>(createdResult.Value);
        Assert.Equal("Oak's Lab", building.Name);
        Assert.Equal(1, building.BuildingId);
        Assert.Equal(1, building.LocationId);
        Assert.Equal(nameof(BuildingsController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateReturnsCreatedAtActionWithGymWhenBuildingTypeIsGym() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pewter City" });

        var newBuilding = new Building {
            Name = "Pewter City Gym",
            BuildingType = BuildingType.Gym,
            Gym = new Gym {
                GymType = "Rock",
                BadgeName = "Boulder Badge",
                GymLeader = "Brock",
                GymOrder = 1
            }
        };

        buildingRepo.CreateAsync(newBuilding).Returns(4);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Create(1, newBuilding);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var building = Assert.IsType<Building>(createdResult.Value);
        Assert.NotNull(building.Gym);
        Assert.Equal("Rock", building.Gym.GymType);
    }

    [Fact]
    public async Task CreateReturnsNotFoundWhenLocationDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Create(1, new Building { Name = "Test" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateReturnsBadRequestWhenModelStateIsInvalid() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        var controller = CreateController(buildingRepo, locationRepo);
        controller.ModelState.AddModelError("Name", "Name is required");

        var result = await controller.Create(1, new Building());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsOkWhenBuildingIsUpdatedSuccessfully() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });

        var building = new Building {
            BuildingId = 1,
            LocationId = 1,
            Name = "Oak's Lab Updated",
            BuildingType = BuildingType.Lab
        };

        buildingRepo.GetByIdAsync(1).Returns(building);
        buildingRepo.UpdateAsync(building).Returns(true);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Update(1, 1, building);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Building>(okResult.Value);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundWhenLocationDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);

        var building = new Building {
            BuildingId = 1,
            LocationId = 1,
            Name = "Test"
        };

        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Update(1, 1, building);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundWhenBuildingDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        buildingRepo.GetByIdAsync(1).Returns((Building?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Update(1, 1, new Building { BuildingId = 1 });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsBadRequestWhenModelStateIsInvalid() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        var controller = CreateController(buildingRepo, locationRepo);
        controller.ModelState.AddModelError("Name", "Name is required");

        var result = await controller.Update(1, 1, new Building { BuildingId = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNoContentWhenBuildingIsDeletedSuccessfully() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        buildingRepo.GetByIdAsync(1).Returns(new Building { BuildingId = 1, LocationId = 1, Name = "Oak's Lab" });
        buildingRepo.DeleteAsync(1).Returns(true);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Delete(1, 1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNotFoundWhenLocationDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Delete(1, 1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNotFoundWhenBuildingDoesNotExist() {
        var buildingRepo = Substitute.For<IBuildingRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        buildingRepo.GetByIdAsync(1).Returns((Building?)null);
        var controller = CreateController(buildingRepo, locationRepo);

        var result = await controller.Delete(1, 1);

        Assert.IsType<NotFoundResult>(result);
    }
}
