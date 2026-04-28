using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Data.Models;
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

    [HttpPost]
    public async Task<IActionResult> Create(int locationId, [FromBody] Building building) {
        logger.LogInformation("Creating a building for location {LocationId}.", locationId);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Create building request for location {LocationId} had an invalid model state.", locationId);
            return BadRequest(ModelState);
        }

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null)
        {
            logger.LogWarning("Cannot create building because location {LocationId} was not found.", locationId);
            return NotFound();
        }

        building.LocationId = locationId;
        var newId = await buildingRepository.CreateAsync(building);
        building.BuildingId = newId;
        logger.LogInformation("Created building {BuildingId} for location {LocationId}.", newId, locationId);
        return CreatedAtAction(nameof(GetById), new { locationId, buildingId = newId }, building);
    }

    [HttpPut("{buildingId}")]
    public async Task<IActionResult> Update(int locationId, int buildingId, [FromBody] Building building) {
        logger.LogInformation("Updating building {BuildingId} for location {LocationId}.", buildingId, locationId);

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Update request for building {BuildingId} in location {LocationId} had an invalid model state.", buildingId, locationId);
            return BadRequest(ModelState);
        }

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null)
        {
            logger.LogWarning("Cannot update building {BuildingId} because location {LocationId} was not found.", buildingId, locationId);
            return NotFound();
        }

        var existing = await buildingRepository.GetByIdAsync(buildingId);
        if (existing == null)
        {
            logger.LogWarning("Cannot update building because building {BuildingId} was not found.", buildingId);
            return NotFound();
        }

        building.LocationId = locationId;
        building.BuildingId = buildingId;
        await buildingRepository.UpdateAsync(building);
        
        logger.LogInformation("Updated building {BuildingId} for location {LocationId}.", buildingId, locationId);
        return Ok(building);
    }

    [HttpDelete("{buildingId}")]
    public async Task<IActionResult> Delete(int locationId, int buildingId) {
        logger.LogInformation("Deleting building {BuildingId} for location {LocationId}.", buildingId, locationId);
        
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null)
        {
            logger.LogWarning("Cannot delete building {BuildingId} because location {LocationId} was not found.", buildingId, locationId);
            return NotFound();
        }

        var existing = await buildingRepository.GetByIdAsync(buildingId);
        if (existing == null)
        {
            logger.LogWarning("Cannot delete building because building {BuildingId} was not found.", buildingId);
            return NotFound();
        }

        await buildingRepository.DeleteAsync(buildingId);
        
        logger.LogInformation("Deleted building {BuildingId} for location {LocationId}.", buildingId, locationId);
        return NoContent();
    }
}
