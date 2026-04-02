using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("locations/{locationId}/[controller]")]
public class BuildingsController : ControllerBase {
    private readonly IBuildingRepository buildingRepository;
    private readonly ILocationRepository locationRepository;

    public BuildingsController(IBuildingRepository buildingRepository, ILocationRepository locationRepository) {
        this.buildingRepository = buildingRepository;
        this.locationRepository = locationRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int locationId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var buildings = await buildingRepository.GetAllByLocationAsync(locationId);
        return Ok(buildings);
    }

    [HttpGet("{buildingId}")]
    public async Task<IActionResult> GetById(int locationId, int buildingId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var building = await buildingRepository.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        return Ok(building);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int locationId, [FromBody] Building building) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        building.LocationId = locationId;
        var newId = await buildingRepository.CreateAsync(building);
        building.BuildingId = newId;
        return CreatedAtAction(nameof(GetById), new { locationId, buildingId = newId }, building);
    }

    [HttpPut("{buildingId}")]
    public async Task<IActionResult> Update(int locationId, int buildingId, [FromBody] Building building) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var existing = await buildingRepository.GetByIdAsync(buildingId);
        if (existing == null) return NotFound();

        building.LocationId = locationId;
        building.BuildingId = buildingId;
        await buildingRepository.UpdateAsync(building);
        return Ok(building);
    }

    [HttpDelete("{buildingId}")]
    public async Task<IActionResult> Delete(int locationId, int buildingId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var existing = await buildingRepository.GetByIdAsync(buildingId);
        if (existing == null) return NotFound();

        await buildingRepository.DeleteAsync(buildingId);
        return NoContent();
    }
}
