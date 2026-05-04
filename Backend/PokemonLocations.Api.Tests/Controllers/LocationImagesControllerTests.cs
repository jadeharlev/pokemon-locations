using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PokemonLocations.Api.Controllers;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Tests.Controllers;

public class LocationImagesControllerTests {
    private static LocationImagesController CreateController(
        ILocationImageRepository imageRepository,
        ILocationRepository locationRepository) {
        return new LocationImagesController(
            imageRepository,
            locationRepository,
            NullLogger<LocationImagesController>.Instance);
    }

    [Fact]
    public async Task GetAllReturnsOkWithImagesWhenLocationExists() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        imageRepo.GetAllByLocationAsync(1).Returns(new List<LocationImage>());
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.GetAll(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsAssignableFrom<IEnumerable<LocationImage>>(okResult.Value);
    }

    [Fact]
    public async Task GetAllReturnsNotFoundWhenLocationDoesNotExist() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.GetAll(1);

        Assert.IsType<NotFoundResult>(result);
    }
}
