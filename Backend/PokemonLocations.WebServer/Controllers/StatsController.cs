using Microsoft.AspNetCore.Mvc;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models.Responses;

namespace PokemonLocations.WebServer.Controllers;

[ApiController]
[Route("/api/me/stats")]
public class StatsController : ControllerBase
{
    private readonly IBadgeRepository badgeRepository;
    private readonly IVisitedBuildingRepository visitedBuildingRepository;
    private readonly IUserRepository userRepository;

    public StatsController(
        IBadgeRepository badgeRepository,
        IVisitedBuildingRepository visitedBuildingRepository,
        IUserRepository userRepository)
    {
        this.badgeRepository = badgeRepository;
        this.visitedBuildingRepository = visitedBuildingRepository;
        this.userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.GetUserId();

        var badgesTask = badgeRepository.GetForUserAsync(userId);
        var visitedLocationsTask = visitedBuildingRepository.GetDistinctLocationIdsForUserAsync(userId);
        var visitedBuildingsTask = visitedBuildingRepository.GetForUserAsync(userId);

        await Task.WhenAll(badgesTask, visitedLocationsTask, visitedBuildingsTask);

        var gymsComplete = badgesTask.Result.Count;
        var locationsVisited = visitedLocationsTask.Result.Count;
        var buildingsVisited = visitedBuildingsTask.Result.Count;

        var max = await userRepository.BumpAndGetMaxStatsAsync(
            userId, gymsComplete, locationsVisited, buildingsVisited);

        return Ok(new StatsResponse(
            gymsComplete,
            locationsVisited,
            buildingsVisited,
            max.MaxGymsComplete,
            max.MaxLocationsVisited,
            max.MaxBuildingsVisited));
    }
}
