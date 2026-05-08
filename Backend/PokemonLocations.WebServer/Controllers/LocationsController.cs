using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Clients;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Extensions;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/locations")]
public class LocationsController : ControllerBase {
    private readonly IPokemonLocationsApiClient apiClient;
    private readonly IVisitedBuildingRepository visitedBuildingRepository;
    private readonly IUserImageRepository userImageRepository;

    public LocationsController(
        IPokemonLocationsApiClient apiClient,
        IVisitedBuildingRepository visitedBuildingRepository,
        IUserImageRepository userImageRepository) {
        this.apiClient = apiClient;
        this.visitedBuildingRepository = visitedBuildingRepository;
        this.userImageRepository = userImageRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() {
        var response = await apiClient.GetWithStatusAsync("/locations");
        if (response.StatusCode != 200) return this.ProxyError(response);

        var locations = JsonNode.Parse(response.Body!)!.AsArray();
        var visitedIds = await visitedBuildingRepository.GetDistinctLocationIdsForUserAsync(User.GetUserId());
        var visitedSet = new HashSet<int>(visitedIds);

        foreach (var location in locations) {
            var id = location!["locationId"]!.GetValue<int>();
            location["visited"] = visitedSet.Contains(id);
        }

        return Content(locations.ToJsonString(), "application/json");
    }

    [HttpGet("{locationId:int}")]
    public async Task<IActionResult> GetById(int locationId) {
        var response = await apiClient.GetWithStatusAsync($"/locations/{locationId}");
        if (response.StatusCode != 200) return this.ProxyError(response);

        var location = JsonNode.Parse(response.Body!)!.AsObject();
        var visitedIds = await visitedBuildingRepository.GetDistinctLocationIdsForUserAsync(User.GetUserId());
        location["visited"] = visitedIds.Contains(locationId);

        var userImages = await userImageRepository.GetForUserAndLocationAsync(User.GetUserId(), locationId);
        var userImagesArray = new JsonArray();
        foreach (var ui in userImages) {
            userImagesArray.Add(new JsonObject {
                ["imageId"] = ui.ImageId.ToString(),
                ["imageUrl"] = $"/api/me/locations/{locationId}/images/{ui.ImageId}",
                ["originalFilename"] = ui.OriginalFilename,
                ["uploadedAt"] = ui.UploadedAt.ToString("o")
            });
        }
        location["userImages"] = userImagesArray;

        return Content(location.ToJsonString(), "application/json");
    }
}
