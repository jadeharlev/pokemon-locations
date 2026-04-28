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

    [Fact]
    public async Task CreateReturnsCreatedAtActionWithNewImage() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        var newImage = new LocationImage { ImageUrl = "/images/x.png" };
        imageRepo.CreateAsync(Arg.Any<LocationImage>()).Returns(42);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Create(1, newImage);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var image = Assert.IsType<LocationImage>(createdResult.Value);
        Assert.Equal(42, image.ImageId);
        Assert.Equal(1, image.LocationId);
    }

    [Fact]
    public async Task CreateReturnsNotFoundWhenLocationDoesNotExist() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Create(1, new LocationImage { ImageUrl = "/images/x.png" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateReturnsBadRequestWhenModelStateIsInvalid() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        var controller = CreateController(imageRepo, locationRepo);
        controller.ModelState.AddModelError("ImageUrl", "ImageUrl is required");

        var result = await controller.Create(1, new LocationImage());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsOkWhenImageExists() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        imageRepo.GetByIdAsync(5).Returns(new LocationImage { ImageId = 5, LocationId = 1, ImageUrl = "/old.png" });
        imageRepo.UpdateAsync(Arg.Any<LocationImage>()).Returns(true);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Update(1, 5, new LocationImage {
            ImageId = 5,
            LocationId = 1,
            ImageUrl = "/new.png"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundWhenLocationDoesNotExist() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns((Location?)null);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Update(1, 5, new LocationImage { ImageUrl = "/x.png" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateReturnsNotFoundWhenImageDoesNotExist() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        imageRepo.GetByIdAsync(5).Returns((LocationImage?)null);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Update(1, 5, new LocationImage { ImageUrl = "/x.png" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNoContentWhenImageIsDeleted() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        imageRepo.GetByIdAsync(5).Returns(new LocationImage { ImageId = 5, LocationId = 1, ImageUrl = "/x.png" });
        imageRepo.DeleteAsync(5).Returns(true);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Delete(1, 5);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteReturnsNotFoundWhenImageDoesNotExist() {
        var imageRepo = Substitute.For<ILocationImageRepository>();
        var locationRepo = Substitute.For<ILocationRepository>();
        locationRepo.GetByIdAsync(1).Returns(new Location { LocationId = 1, Name = "Pallet Town" });
        imageRepo.GetByIdAsync(5).Returns((LocationImage?)null);
        var controller = CreateController(imageRepo, locationRepo);

        var result = await controller.Delete(1, 5);

        Assert.IsType<NotFoundResult>(result);
    }
}
