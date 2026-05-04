using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PokemonLocations.Api.Controllers;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Tests.Controllers;

public class LocationsControllerTests {
    private static LocationsController CreateController(ILocationRepository repo) {
        return new LocationsController(
            repo,
            NullLogger<LocationsController>.Instance);
    }

    [Fact]
    public async Task GetAllLocationsReturnsOkWithListOfLocations() {
        var repo = Substitute.For<ILocationRepository>();
        repo.GetAllAsync().Returns(new List<Location>());
        var controller = CreateController(repo);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<List<Location>>(okResult.Value);
    }

    [Fact]
    public async Task GetByIdReturnsOkWithLocationWhenLocationExists() {
        var repo = Substitute.For<ILocationRepository>();
        var expected = new Location { LocationId = 1, Name = "Pallet Town" };
        repo.GetByIdAsync(1).Returns(expected);
        var controller = CreateController(repo);

        var result = await controller.GetById(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var location = Assert.IsType<Location>(okResult.Value);
        Assert.Equal(expected, location);
    }

    [Fact]
    public async Task GetByIdReturnsNotFoundWhenLocationDoesNotExist() {
        var repo = Substitute.For<ILocationRepository>();
        repo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(repo);

        var result = await controller.GetById(1);

        Assert.IsType<NotFoundResult>(result);
    }
}
