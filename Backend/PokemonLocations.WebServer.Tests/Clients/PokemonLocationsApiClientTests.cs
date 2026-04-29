using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Tests.Clients;

public class PokemonLocationsApiClientTests {
    [Fact]
    public async Task GetAsyncAttachesBearerTokenFromProvider() {
        var handler = new RecordingHttpMessageHandler();
        var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("http://api.test")
        };
        var tokenProvider = Substitute.For<IJwtTokenProvider>();
        tokenProvider.GetCurrentToken().Returns("the-token");
        var client = new PokemonLocationsApiClient(httpClient, tokenProvider);

        await client.GetAsync("/locations");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("the-token", handler.LastRequest!.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetAsyncReadsTokenForEveryRequest() {
        var handler = new RecordingHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api.test") };
        var tokenProvider = Substitute.For<IJwtTokenProvider>();
        tokenProvider.GetCurrentToken().Returns("token-1", "token-2");
        var client = new PokemonLocationsApiClient(httpClient, tokenProvider);

        await client.GetAsync("/locations");
        await client.GetAsync("/buildings");

        tokenProvider.Received(2).GetCurrentToken();
    }

    [Fact]
    public async Task GetAsyncReturnsResponseBody() {
        var handler = new RecordingHttpMessageHandler {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent.Create(new[] { new { name = "Pallet Town" } })
            }
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api.test") };
        var tokenProvider = Substitute.For<IJwtTokenProvider>();
        tokenProvider.GetCurrentToken().Returns("the-token");
        var client = new PokemonLocationsApiClient(httpClient, tokenProvider);

        var body = await client.GetAsync("/locations");

        Assert.Contains("Pallet Town", body);
    }

    [Fact]
    public async Task ExistsAsyncReturnsTrueFor200() {
        var handler = new RecordingHttpMessageHandler {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        };
        var client = CreateClient(handler);

        Assert.True(await client.ExistsAsync("/locations/1"));
    }

    [Fact]
    public async Task ExistsAsyncReturnsFalseFor404() {
        var handler = new RecordingHttpMessageHandler {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
        var client = CreateClient(handler);

        Assert.False(await client.ExistsAsync("/locations/999"));
    }

    [Fact]
    public async Task ExistsAsyncRethrowsOtherErrors() {
        var handler = new RecordingHttpMessageHandler {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.ExistsAsync("/locations/1"));
    }

    [Fact]
    public async Task ExistsAsyncAttachesBearerToken() {
        var handler = new RecordingHttpMessageHandler {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
        };
        var client = CreateClient(handler, "exists-token");

        await client.ExistsAsync("/locations/1");

        Assert.Equal("exists-token", handler.LastRequest!.Headers.Authorization?.Parameter);
    }

    private static PokemonLocationsApiClient CreateClient(
        RecordingHttpMessageHandler handler, string token = "the-token") {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api.test") };
        var tokenProvider = Substitute.For<IJwtTokenProvider>();
        tokenProvider.GetCurrentToken().Returns(token);
        return new PokemonLocationsApiClient(httpClient, tokenProvider);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler {
        public HttpRequestMessage? LastRequest { get; private set; }
        public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequest = request;
            return Task.FromResult(ResponseFactory(request));
        }
    }
}
