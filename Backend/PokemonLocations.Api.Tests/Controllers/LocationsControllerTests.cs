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

    [Fact]
    public async Task CreateReturnsCreatedAtActionWithNewLocation() {
        var repo = Substitute.For<ILocationRepository>();
        var newLocation = new Location { Name = "Viridian City" };
        repo.CreateAsync(newLocation).Returns(1);
        var controller = CreateController(repo);

        var result = await controller.Create(newLocation);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var location = Assert.IsType<Location>(createdResult.Value);
        Assert.Equal("Viridian City", location.Name);
        Assert.Equal(1, location.LocationId);
        Assert.Equal(nameof(LocationsController.GetById), createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues!["locationId"]);
    }

    [Fact]
    public async Task CreateReturnsBadRequestWhenModelStateIsInvalid() {
        var repo = Substitute.For<ILocationRepository>();
        var controller = CreateController(repo);
        controller.ModelState.AddModelError("Name", "Name is required");

        var result = await controller.Create(new Location());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsOkWhenLocationIsUpdatedSuccessfully() {
        var repo = Substitute.For<ILocationRepository>();
        var location = new Location { LocationId = 1, Name = "Pallet Town Updated" };
        repo.GetByIdAsync(1).Returns(location);
        repo.UpdateAsync(location).Returns(true);
        var controller = CreateController(repo);

        var result = await controller.Update(1, location);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Location>(okResult.Value);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundWhenLocationDoesNotExist() {
        var repo = Substitute.For<ILocationRepository>();
        var location = new Location { LocationId = 1, Name = "Nonexistent" };
        repo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(repo);

        var result = await controller.Update(1, location);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsBadRequestWhenModelStateIsInvalid() {
        var repo = Substitute.For<ILocationRepository>();
        var controller = CreateController(repo);
        controller.ModelState.AddModelError("Name", "Name is required");

        var result = await controller.Update(1, new Location { LocationId = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNoContentWhenLocationIsDeletedSuccessfully() {
        var repo = Substitute.For<ILocationRepository>();
        repo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        repo.DeleteAsync(1).Returns(true);
        var controller = CreateController(repo);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNotFoundWhenLocationDoesNotExist() {
        var repo = Substitute.For<ILocationRepository>();
        repo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(repo);

        var result = await controller.Delete(1);

        Assert.IsType<NotFoundResult>(result);
    }
}
