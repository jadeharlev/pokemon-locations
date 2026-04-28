using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PokemonLocations.Api.Data.Models;
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

    [HttpPost]
    public async Task<IActionResult> Create(int locationId, [FromBody] LocationImage image) {
        logger.LogInformation("Creating image for location {LocationId}.", locationId);

        if (!ModelState.IsValid) {
            logger.LogWarning("Invalid model state while creating image for location {LocationId}.", locationId);
            return BadRequest(ModelState);
        }

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) {
            logger.LogWarning("Cannot create image because location {LocationId} was not found.", locationId);
            return NotFound();
        }

        image.LocationId = locationId;
        var newId = await imageRepository.CreateAsync(image);
        image.ImageId = newId;

        logger.LogInformation("Successfully created image {ImageId} for location {LocationId}.", newId, locationId);
        return CreatedAtAction(nameof(GetById), new { locationId, imageId = newId }, image);
    }

    [HttpPut("{imageId}")]
    public async Task<IActionResult> Update(int locationId, int imageId, [FromBody] LocationImage image) {
        logger.LogInformation("Updating image {ImageId} for location {LocationId}.", imageId, locationId);

        if (!ModelState.IsValid) {
            logger.LogWarning("Invalid model state while updating image {ImageId} for location {LocationId}.", imageId, locationId);
            return BadRequest(ModelState);
        }

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) {
            logger.LogWarning("Cannot update image {ImageId} because location {LocationId} was not found.", imageId, locationId);
            return NotFound();
        }

        var existing = await imageRepository.GetByIdAsync(imageId);
        if (existing == null) {
            logger.LogWarning("Cannot update image because image {ImageId} was not found.", imageId);
            return NotFound();
        }

        image.LocationId = locationId;
        image.ImageId = imageId;

        await imageRepository.UpdateAsync(image);

        logger.LogInformation("Successfully updated image {ImageId} for location {LocationId}.", imageId, locationId);
        return Ok(image);
    }

    [HttpDelete("{imageId}")]
    public async Task<IActionResult> Delete(int locationId, int imageId) {
        logger.LogInformation("Deleting image {ImageId} from location {LocationId}.", imageId, locationId);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) {
            logger.LogWarning("Cannot delete image {ImageId} because location {LocationId} was not found.", imageId, locationId);
            return NotFound();
        }

        var existing = await imageRepository.GetByIdAsync(imageId);
        if (existing == null) {
            logger.LogWarning("Cannot delete image because image {ImageId} was not found.", imageId);
            return NotFound();
        }

        await imageRepository.DeleteAsync(imageId);

        logger.LogInformation("Successfully deleted image {ImageId} from location {LocationId}.", imageId, locationId);
        return NoContent();
    }
}
