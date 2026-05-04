using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
}
