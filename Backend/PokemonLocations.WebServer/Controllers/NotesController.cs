using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models.Requests;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/notes")]
public class NotesController : ControllerBase {
    private const int MaxNoteLength = 10_000;

    private readonly IUserNoteRepository noteRepository;
    private readonly IPokemonLocationsApiClient apiClient;

    public NotesController(IUserNoteRepository noteRepository, IPokemonLocationsApiClient apiClient) {
        this.noteRepository = noteRepository;
        this.apiClient = apiClient;
    }

    [HttpGet("{locationId:int}")]
    public async Task<IActionResult> Get(int locationId) {
        var noteText = await noteRepository.GetAsync(User.GetUserId(), locationId);
        if (noteText is null) {
            return NotFound(new { error = "not_found" });
        }
        return Ok(new { noteText });
    }

    [HttpPut("{locationId:int}")]
    public async Task<IActionResult> Put(int locationId, [FromBody] UpsertNoteRequest request) {
        if (string.IsNullOrWhiteSpace(request.NoteText)) {
            return BadRequest(new { error = "empty_note" });
        }
        if (request.NoteText.Length > MaxNoteLength) {
            return BadRequest(new { error = "note_too_long" });
        }
        if (!await apiClient.ExistsAsync($"/locations/{locationId}")) {
            return NotFound(new { error = "not_found" });
        }
        await noteRepository.UpsertAsync(User.GetUserId(), locationId, request.NoteText);
        return NoContent();
    }

    [HttpDelete("{locationId:int}")]
    public async Task<IActionResult> Delete(int locationId) {
        await noteRepository.DeleteAsync(User.GetUserId(), locationId);
        return NoContent();
    }
}
