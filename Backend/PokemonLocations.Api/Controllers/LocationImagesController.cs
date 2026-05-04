using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("locations/{locationId}/images")]
public class LocationImagesController : ControllerBase {
    private readonly ILocationImageRepository imageRepository;
    private readonly ILocationRepository locationRepository;
    private readonly ILogger<LocationImagesController> logger;

    public LocationImagesController(
        ILocationImageRepository imageRepository,
        ILocationRepository locationRepository,
        ILogger<LocationImagesController> logger) {
        this.imageRepository = imageRepository;
        this.locationRepository = locationRepository;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int locationId) {
        logger.LogInformation("Getting all images for location {LocationId}.", locationId);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) {
            logger.LogWarning("Cannot get images because location {LocationId} was not found.", locationId);
            return NotFound();
        }

        var images = await imageRepository.GetAllByLocationAsync(locationId);

        logger.LogInformation("Successfully retrieved images for location {LocationId}.", locationId);
        return Ok(images);
    }

    [HttpGet("{imageId}")]
    public async Task<IActionResult> GetById(int locationId, int imageId) {
        logger.LogInformation("Getting image {ImageId} for location {LocationId}.", imageId, locationId);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) {
            logger.LogWarning("Cannot get image {ImageId} because location {LocationId} was not found.", imageId, locationId);
            return NotFound();
        }

        var image = await imageRepository.GetByIdAsync(imageId);
        if (image == null) {
            logger.LogWarning("Image {ImageId} was not found.", imageId);
            return NotFound();
        }

        logger.LogInformation("Successfully retrieved image {ImageId} for location {LocationId}.", imageId, locationId);
        return Ok(image);
    }
}
