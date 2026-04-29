using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/gyms")]
public class GymsController : ControllerBase {
    private readonly IPokemonLocationsApiClient apiClient;

    public GymsController(IPokemonLocationsApiClient apiClient) {
        this.apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        var response = await apiClient.GetWithStatusAsync("/gyms");
        if (response.StatusCode != 200) {
            if (response.StatusCode == 404) return NotFound();
            return StatusCode(502);
        }

        return Content(response.Body!, "application/json");
    }
}
