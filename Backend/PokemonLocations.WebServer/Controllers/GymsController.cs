using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Extensions;

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
        if (response.StatusCode != 200) return this.ProxyError(response);

        return Content(response.Body!, "application/json");
    }
}
