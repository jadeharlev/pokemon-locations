using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Data.Models;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("locations/{locationId}/images")]
public class LocationImagesController : ControllerBase {
    private readonly ILocationImageRepository imageRepository;
    private readonly ILocationRepository locationRepository;

    public LocationImagesController(
        ILocationImageRepository imageRepository,
        ILocationRepository locationRepository) {
        this.imageRepository = imageRepository;
        this.locationRepository = locationRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int locationId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var images = await imageRepository.GetAllByLocationAsync(locationId);
        return Ok(images);
    }

    [HttpGet("{imageId}")]
    public async Task<IActionResult> GetById(int locationId, int imageId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var image = await imageRepository.GetByIdAsync(imageId);
        if (image == null) return NotFound();

        return Ok(image);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int locationId, [FromBody] LocationImage image) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        image.LocationId = locationId;
        var newId = await imageRepository.CreateAsync(image);
        image.ImageId = newId;
        return CreatedAtAction(nameof(GetById), new { locationId, imageId = newId }, image);
    }

    [HttpPut("{imageId}")]
    public async Task<IActionResult> Update(int locationId, int imageId, [FromBody] LocationImage image) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var existing = await imageRepository.GetByIdAsync(imageId);
        if (existing == null) return NotFound();

        image.LocationId = locationId;
        image.ImageId = imageId;
        await imageRepository.UpdateAsync(image);
        return Ok(image);
    }

    [HttpDelete("{imageId}")]
    public async Task<IActionResult> Delete(int locationId, int imageId) {
        var location = await locationRepository.GetByIdAsync(locationId);
        if (location == null) return NotFound();

        var existing = await imageRepository.GetByIdAsync(imageId);
        if (existing == null) return NotFound();

        await imageRepository.DeleteAsync(imageId);
        return NoContent();
    }
}
