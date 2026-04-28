using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class GymsController : ControllerBase {
    private readonly IGymRepository gymRepository;
    private readonly ILogger<GymsController> logger;

    public GymsController(IGymRepository gymRepository, ILogger<GymsController> logger) {
        this.gymRepository = gymRepository;
	this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        logger.LogInformation("Getting all gyms.");

        var gyms = await gymRepository.GetAllAsync();
        
	logger.LogInformation("All gyms retrieved.");
	return Ok(gyms);
    }

    [HttpGet("{gymId}")]
    public async Task<IActionResult> GetById(int gymId) {
	logger.LogInformation("Getting gym with ID {GymId}.", gymId);
        
	var gym = await gymRepository.GetByIdAsync(gymId);
        if (gym == null)
	{
		logger.LogWarning("Gym with ID {GymId} was not found.", gymId);
 		return NotFound();
	}
	logger.LogInformation("Successfully retrieved all gyms.");
        return Ok(gym);
    }
}
