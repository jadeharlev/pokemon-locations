using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class LocationsController : ControllerBase {
    private readonly ILocationRepository locationRepository;

    public LocationsController(ILocationRepository locationRepository) {
        this.locationRepository = locationRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        var locations = await locationRepository.GetAllAsync();
        return Ok(locations);
    }

    [HttpGet("{locationId}")]
    public async Task<IActionResult> GetById(int locationId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();
        return Ok(location);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Location location) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var newId = await locationRepository.CreateAsync(location);
        location.LocationId = newId;
        return CreatedAtAction(nameof(GetById), new { locationId = newId }, location);
    }

    [HttpPut("{locationId}")]
    public async Task<IActionResult> Update(int locationId, [FromBody] Location location) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existing = await locationRepository.GetByIdAsync(locationId);
        if (existing == null) return NotFound();

        await locationRepository.UpdateAsync(location);
        return Ok(location);
    }

    [HttpDelete("{locationId}")]
    public async Task<IActionResult> Delete(int locationId) {
        var existing = await locationRepository.GetByIdAsync(locationId);
        if (existing == null) return NotFound();

        await locationRepository.DeleteAsync(locationId);
        return NoContent();
    }
}
