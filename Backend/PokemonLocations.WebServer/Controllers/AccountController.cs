using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models.Requests;
using PokemonLocations.WebServer.Models.Responses;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
public class AccountController : ControllerBase {
    private const string UniqueViolationSQLState = "23505";

    private readonly IUserRepository userRepository;
    private readonly PasswordHasher passwordHasher;
    private readonly IStarTrekWeatherApiClient weatherClient;

    public AccountController(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        IStarTrekWeatherApiClient weatherClient) {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.weatherClient = weatherClient;
    }

    [HttpPost("/account/signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var hash = passwordHasher.HashPassword(request.Password);
        try {
            var user = await userRepository.CreateAsync(
                EmailNormalizer.Normalize(request.Email),
                hash,
                request.DisplayName.Trim());
            return Created("/api/me", MeResponse.FromUser(user));
        } catch (PostgresException exception) when (exception.SqlState == UniqueViolationSQLState) {
            return Conflict(new { error = "email_taken" });
        }
    }

    [HttpDelete("/account")]
    public async Task<IActionResult> Delete() {
        await userRepository.DeleteAsync(User.GetUserId());
        return NoContent();
    }

    [HttpPut("/account/theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeRequest request) {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await userRepository.UpdateThemeAsync(User.GetUserId(), request.Theme);
        return NoContent();
    }

    [HttpPut("/account/permanent-planet")]
    public async Task<IActionResult> UpdatePermanentPlanet(
        [FromBody] UpdatePermanentPlanetRequest request,
        CancellationToken ct) {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var planet = await weatherClient.GetByNameAsync(request.PlanetName, ct);
        if (planet is null) {
            return BadRequest(new { error = "unknown_planet" });
        }
        await userRepository.UpdatePermanentPlanetAsync(User.GetUserId(), planet.Name);
        return NoContent();
    }

    [HttpGet("/api/me")]
    public async Task<IActionResult> Me() {
        var user = await userRepository.GetByIdAsync(User.GetUserId());
        if (user is null) return NotFound();
        return Ok(MeResponse.FromUser(user));
    }
}
