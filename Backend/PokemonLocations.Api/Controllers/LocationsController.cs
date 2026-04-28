using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class LocationsController : ControllerBase {
    private readonly ILocationRepository locationRepository;
    private readonly ILogger<LocationsController> logger;

    public LocationsController(
        ILocationRepository locationRepository,
        ILogger<LocationsController> logger) {
        this.locationRepository = locationRepository;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        logger.LogInformation("Getting all locations.");

        var locations = await locationRepository.GetAllAsync();

        logger.LogInformation("Successfully retrieved all locations.");
        return Ok(locations);
    }

    [HttpGet("{locationId}")]
    public async Task<IActionResult> GetById(int locationId) {
        logger.LogInformation("Getting location with ID {LocationId}.", locationId);

        var location = await locationRepository.GetByIdAsync(locationId);

        if (location == null) {
            logger.LogWarning("Location with ID {LocationId} was not found.", locationId);
            return NotFound();
        }

        logger.LogInformation("Successfully retrieved location with ID {LocationId}.", locationId);
        return Ok(location);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Location location) {
        logger.LogInformation("Creating a new location.");

        if (!ModelState.IsValid) {
            logger.LogWarning("Invalid model state while creating a location.");
            return BadRequest(ModelState);
        }

        var newId = await locationRepository.CreateAsync(location);
        location.LocationId = newId;

        logger.LogInformation("Successfully created location with ID {LocationId}.", newId);
        return CreatedAtAction(nameof(GetById), new { locationId = newId }, location);
    }

    [HttpPut("{locationId}")]
    public async Task<IActionResult> Update(int locationId, [FromBody] Location location) {
        logger.LogInformation("Updating location with ID {LocationId}.", locationId);

        if (!ModelState.IsValid) {
            logger.LogWarning("Invalid model state while updating location with ID {LocationId}.", locationId);
            return BadRequest(ModelState);
        }

        var existing = await locationRepository.GetByIdAsync(locationId);

        if (existing == null) {
            logger.LogWarning("Cannot update location because location with ID {LocationId} was not found.", locationId);
            return NotFound();
        }

        location.LocationId = locationId;

        await locationRepository.UpdateAsync(location);

        logger.LogInformation("Successfully updated location with ID {LocationId}.", locationId);
        return Ok(location);
    }

    [HttpDelete("{locationId}")]
    public async Task<IActionResult> Delete(int locationId) {
        logger.LogInformation("Deleting location with ID {LocationId}.", locationId);

        var existing = await locationRepository.GetByIdAsync(locationId);

        if (existing == null) {
            logger.LogWarning("Cannot delete location because location with ID {LocationId} was not found.", locationId);
            return NotFound();
        }

        await locationRepository.DeleteAsync(locationId);

        logger.LogInformation("Successfully deleted location with ID {LocationId}.", locationId);
        return NoContent();
    }
}
