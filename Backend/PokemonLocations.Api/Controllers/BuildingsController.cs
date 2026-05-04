using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("locations/{locationId}/[controller]")]
public class BuildingsController : ControllerBase {
    private readonly IBuildingRepository buildingRepository;
    private readonly ILocationRepository locationRepository;
    private readonly ILogger<BuildingsController> logger;

    public BuildingsController(IBuildingRepository buildingRepository, ILocationRepository locationRepository, ILogger<BuildingsController> logger) {
        this.buildingRepository = buildingRepository;
        this.locationRepository = locationRepository;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int locationId) {
        logger.LogInformation("Getting all buildings for location {LocationId}.", locationId);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null)
        {
            logger.LogWarning("Cannot get buildings because location {LocationId} was not found.", locationId);
            return NotFound();
        }

        var buildings = await buildingRepository.GetAllByLocationAsync(locationId);
        logger.LogInformation("Retrieved buildings for location {LocationId}.", locationId);
        return Ok(buildings);
    }

    [HttpGet("{buildingId}")]
    public async Task<IActionResult> GetById(int locationId, int buildingId) {
        logger.LogInformation("Getting building {BuildingId} for location {LocationId}.", buildingId, locationId);
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null)
        {
            logger.LogWarning("Cannot get building {BuildingId} because location {LocationId} was not found.", buildingId, locationId);
            return NotFound();
        }

        var building = await buildingRepository.GetByIdAsync(buildingId);
        if (building == null)
        {
            logger.LogWarning("Building {BuildingId} was not found for location {LocationId}.", buildingId, locationId);
            return NotFound();
        }
        logger.LogInformation("Retrieved building {BuildingId} for location {LocationId}.", buildingId, locationId);
        return Ok(building);
    }
}
