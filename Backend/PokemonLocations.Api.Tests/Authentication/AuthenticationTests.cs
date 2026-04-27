using System.Net;
using System.Net.Http.Headers;
using PokemonLocations.Api.Tests.Infrastructure;

namespace PokemonLocations.Api.Tests.Authentication;

[Collection("Postgres")]
public class AuthenticationTests : IAsyncLifetime {
    private readonly PostgresFixture postgresFixture;
    private PokemonLocationsApiFactory factory = null!;
    private HttpClient client = null!;

    public AuthenticationTests(PostgresFixture postgresFixture) {
        this.postgresFixture = postgresFixture;
    }

    public Task InitializeAsync() {
        factory = new PokemonLocationsApiFactory(postgresFixture.ConnectionString);
        client = factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() {
        client?.Dispose();
        factory?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetLocationsWithoutAuthorizationHeaderReturnsUnauthorized() {
        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationsWithValidBearerTokenReturnsOk() {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenTestHelper.Create());

        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthDbWithoutAuthorizationHeaderReturnsOk() {
        var response = await client.GetAsync("/health/db");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationsWithMalformedTokenReturnsUnauthorized() {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationsWithWrongIssuerReturnsUnauthorized() {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenTestHelper.Create(issuer: "some-other-issuer"));

        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationsWithWrongAudienceReturnsUnauthorized() {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenTestHelper.Create(audience: "some-other-audience"));

        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationsWithExpiredTokenReturnsUnauthorized() {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenTestHelper.Create(expires: DateTime.UtcNow.AddMinutes(-5)));

        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationsWithWrongSigningKeyReturnsUnauthorized() {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenTestHelper.Create(key: "completely-different-signing-key-32-bytes-or-more!!"));

        var response = await client.GetAsync("/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
