using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/badges")]
public class BadgeController : ControllerBase {
    private readonly IBadgeRepository badgeRepository;

    public BadgeController(IBadgeRepository badgeRepository) {
        this.badgeRepository = badgeRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get() {
        var badges = await badgeRepository.GetForUserAsync(User.GetUserId());
        return Ok(badges);
    }

    [HttpPut("{badge}")]
    public async Task<IActionResult> Put(string badge) {
        if (!BadgeParser.TryParse(badge, out var parsed)) {
            return BadRequest(new { error = "invalid_badge" });
        }
        await badgeRepository.AddAsync(User.GetUserId(), BadgeParser.ToWireFormat(parsed));
        return NoContent();
    }

    [HttpDelete("{badge}")]
    public async Task<IActionResult> Delete(string badge) {
        if (!BadgeParser.TryParse(badge, out var parsed)) {
            return BadRequest(new { error = "invalid_badge" });
        }
        await badgeRepository.RemoveAsync(User.GetUserId(), BadgeParser.ToWireFormat(parsed));
        return NoContent();
    }
}
