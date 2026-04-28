using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PokemonLocations.Api.Controllers;
using PokemonLocations.Api.Repositories;

namespace PokemonLocations.Api.Tests.Controllers;

public class HealthControllerTests {
    private static HealthController CreateController(IDatabaseHealthRepository repo) {
        return new HealthController(
            repo,
            NullLogger<HealthController>.Instance);
    }

    [Fact]
    public async Task GetHealthReturnsOkWhenSuccessful() {
        var repo = Substitute.For<IDatabaseHealthRepository>();
        repo.GetHealth().Returns(true);
        var controller = CreateController(repo);

        var result = await controller.CheckDatabaseHealth();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Database Connected", okResult.Value);
    }

    [Fact]
    public async Task GetHealthReturns500WhenUnsuccessful() {
        var repo = Substitute.For<IDatabaseHealthRepository>();
        repo.GetHealth().Returns(false);
        var controller = CreateController(repo);

        var result = await controller.CheckDatabaseHealth();

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
