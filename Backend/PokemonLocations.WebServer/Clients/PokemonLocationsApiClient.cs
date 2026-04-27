using System.Net.Http.Headers;

namespace PokemonLocations.WebServer.Clients;

public class PokemonLocationsApiClient : IPokemonLocationsApiClient {
    private readonly HttpClient httpClient;
    private readonly IJwtTokenProvider tokenProvider;

    public PokemonLocationsApiClient(HttpClient httpClient, IJwtTokenProvider tokenProvider) {
        this.httpClient = httpClient;
        this.tokenProvider = tokenProvider;
    }

    public async Task<string> GetAsync(string path) {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.GetCurrentToken());
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
