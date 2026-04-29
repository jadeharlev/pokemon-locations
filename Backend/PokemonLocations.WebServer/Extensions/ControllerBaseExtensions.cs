using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Extensions;

public static class ControllerBaseExtensions {
    public static IActionResult ProxyError(this ControllerBase controller, ApiResponse response) {
        if (response.StatusCode == 404) return controller.NotFound();
        return controller.StatusCode(502);
    }
}
