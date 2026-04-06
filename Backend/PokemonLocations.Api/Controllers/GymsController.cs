using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class GymsController : ControllerBase {
    private readonly IGymRepository gymRepository;

    public GymsController(IGymRepository gymRepository) {
        this.gymRepository = gymRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        var gyms = await gymRepository.GetAllAsync();
        return Ok(gyms);
    }

    [HttpGet("{gymId}")]
    public async Task<IActionResult> GetById(int gymId) {
        var gym = await gymRepository.GetByIdAsync(gymId);
        if (gym == null) return NotFound();

        return Ok(gym);
    }
}
