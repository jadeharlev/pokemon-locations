using System.Net;
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
        using var response = await SendAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<ApiResponse> GetWithStatusAsync(string path) {
        using var response = await SendAsync(path);
        var body = response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : null;
        return new ApiResponse((int)response.StatusCode, body);
    }

    public async Task<bool> ExistsAsync(string path) {
        using var response = await SendAsync(path);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<HttpResponseMessage> SendAsync(string path) {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.GetCurrentToken());
        return await httpClient.SendAsync(request);
    }
}
