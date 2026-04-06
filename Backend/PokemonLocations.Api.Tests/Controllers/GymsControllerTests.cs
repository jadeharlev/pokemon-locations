using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PokemonLocations.Api.Controllers;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Tests.Controllers;

public class GymsControllerTests {
    [Fact]
    public async Task GetAllReturnsOkWithListOfGymSummaries() {
        var gymRepo = Substitute.For<IGymRepository>();
        gymRepo.GetAllAsync().Returns(new List<GymSummary>());
        var controller = new GymsController(gymRepo);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsAssignableFrom<IEnumerable<GymSummary>>(okResult.Value);
    }

    [Fact]
    public async Task GetAllReturnsGymsOrderedByGymOrder() {
        var gymRepo = Substitute.For<IGymRepository>();
        var gyms = new List<GymSummary> {
            new() { GymId = 1, GymOrder = 1, GymLeader = "Brock" },
            new() { GymId = 2, GymOrder = 2, GymLeader = "Misty" },
            new() { GymId = 3, GymOrder = 3, GymLeader = "Lt. Surge" }
        };
        gymRepo.GetAllAsync().Returns(gyms);
        var controller = new GymsController(gymRepo);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<GymSummary>>(okResult.Value);
        var ordered = payload.ToList();
        Assert.Equal(new[] { 1, 2, 3 }, ordered.Select(g => g.GymOrder));
    }

    [Fact]
    public async Task GetByIdReturnsOkWithGymSummaryWhenGymExists() {
        var gymRepo = Substitute.For<IGymRepository>();
        var expected = new GymSummary {
            GymId = 1,
            BuildingId = 4,
            LocationId = 2,
            LocationName = "Pewter City",
            BuildingName = "Pewter City Gym",
            GymType = "Rock",
            BadgeName = "Boulder Badge",
            GymLeader = "Brock",
            GymOrder = 1
        };
        gymRepo.GetByIdAsync(1).Returns(expected);
        var controller = new GymsController(gymRepo);

        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var gym = Assert.IsType<GymSummary>(okResult.Value);
        Assert.Equal(expected, gym);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundWhenGymDoesNotExist() {
        var gymRepo = Substitute.For<IGymRepository>();
        gymRepo.GetByIdAsync(999).Returns((GymSummary?)null);
        var controller = new GymsController(gymRepo);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
