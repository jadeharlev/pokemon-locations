using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models.Requests;
using PokemonLocations.WebServer.Models.Responses;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
public class AccountController : ControllerBase {
    private const string UniqueViolationSQLState = "23505";

    private readonly IUserRepository userRepository;
    private readonly PasswordHasher passwordHasher;

    public AccountController(IUserRepository userRepository, PasswordHasher passwordHasher) {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
    }

    [HttpPost("/account/signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request) {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var hash = passwordHasher.HashPassword(request.Password);
        try {
            var user = await userRepository.CreateAsync(
                request.Email,
                hash,
                request.DisplayName.Trim());
            return Created("/api/me", MeResponse.FromUser(user));
        } catch (PostgresException exception) when (exception.SqlState == UniqueViolationSQLState) {
            return Conflict(new { error = "email_taken" });
        }
    }

    [HttpDelete("/account")]
    public async Task<IActionResult> Delete() {
        var userId = GetUserId();
        await userRepository.DeleteAsync(userId);
        return NoContent();
    }

    [HttpGet("/api/me")]
    public async Task<IActionResult> Me() {
        var userId = GetUserId();
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null) return NotFound();
        return Ok(MeResponse.FromUser(user));
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
