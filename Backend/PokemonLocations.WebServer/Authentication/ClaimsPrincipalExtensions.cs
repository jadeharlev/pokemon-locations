using System.Security.Claims;

namespace PokemonLocations.WebServer.Authentication;

public static class ClaimsPrincipalExtensions {
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
