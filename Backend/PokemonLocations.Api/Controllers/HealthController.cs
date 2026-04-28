using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase {
    private readonly IDatabaseHealthRepository databaseHealthRepository;
    private readonly ILogger<HealthController> logger;
    public HealthController(IDatabaseHealthRepository databaseHealthRepository, ILogger<HealthController> logger) {
        this.databaseHealthRepository = databaseHealthRepository;
	this.logger = logger;
    }

    [HttpGet("db")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckDatabaseHealth() {
        logger.LogInformation("Checking database health.");

        bool success = await databaseHealthRepository.GetHealth();
	if (success) {
            logger.LogInformation("Database health check passed.");
            return Ok("Database Connected");
        }

	logger.LogError("Database health check failed.");
        return StatusCode(500);
    }
}
