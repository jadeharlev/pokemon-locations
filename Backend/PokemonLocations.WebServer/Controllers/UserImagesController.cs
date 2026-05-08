using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;
using PokemonLocations.WebServer.Services;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/locations/{locationId:int}/images")]
public class UserImagesController : ControllerBase {
    private readonly IUserImageRepository repository;
    private readonly IImageProcessor processor;
    private readonly IPokemonLocationsApiClient apiClient;
    private readonly UserImagesOptions options;
    private readonly ILogger<UserImagesController> logger;

    public UserImagesController(
        IUserImageRepository repository,
        IImageProcessor processor,
        IPokemonLocationsApiClient apiClient,
        IOptions<UserImagesOptions> options,
        ILogger<UserImagesController> logger) {
        this.repository = repository;
        this.processor = processor;
        this.apiClient = apiClient;
        this.options = options.Value;
        this.logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> Upload(int locationId, IFormFile file) =>
        throw new NotImplementedException();

    [HttpDelete("{imageId:guid}")]
    public Task<IActionResult> Delete(int locationId, Guid imageId) =>
        throw new NotImplementedException();

    [HttpGet("{imageId:guid}")]
    public Task<IActionResult> Get(int locationId, Guid imageId) =>
        throw new NotImplementedException();
}
