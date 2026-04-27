using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase {
    private readonly IDatabaseHealthRepository databaseHealthRepository;

    public HealthController(IDatabaseHealthRepository databaseHealthRepository) {
        this.databaseHealthRepository = databaseHealthRepository;
    }

    [HttpGet("db")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckDatabaseHealth() {
        bool success = await databaseHealthRepository.GetHealth();
        return success ? Ok("Database Connected") : StatusCode(500);
    }
}
