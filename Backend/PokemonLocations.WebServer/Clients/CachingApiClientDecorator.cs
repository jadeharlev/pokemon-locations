using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace PokemonLocations.WebServer.Clients;

public class CachingApiClientDecorator : IPokemonLocationsApiClient {
    private readonly IPokemonLocationsApiClient inner;
    private readonly IDistributedCache cache;
    private readonly TimeSpan ttl;

    public CachingApiClientDecorator(IPokemonLocationsApiClient inner, IDistributedCache cache, TimeSpan ttl) {
        this.inner = inner;
        this.cache = cache;
        this.ttl = ttl;
    }

    public async Task<string> GetAsync(string path) {
        var cached = await cache.GetAsync(path);
        if (cached is not null) {
            return Encoding.UTF8.GetString(cached);
        }

        var body = await inner.GetAsync(path);
        await cache.SetAsync(path, Encoding.UTF8.GetBytes(body), new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = ttl
        });
        return body;
    }

    public async Task<ApiResponse> GetWithStatusAsync(string path) {
        var cached = await cache.GetAsync(path);
        if (cached is not null) {
            return new ApiResponse(200, Encoding.UTF8.GetString(cached));
        }

        var result = await inner.GetWithStatusAsync(path);
        if (result.StatusCode == 200 && result.Body is not null) {
            await cache.SetAsync(path, Encoding.UTF8.GetBytes(result.Body), new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = ttl
            });
        }
        return result;
    }

    public async Task<bool> ExistsAsync(string path) {
        var key = "exists:" + path;
        var cached = await cache.GetAsync(key);
        if (cached is { Length: > 0 }) {
            return cached[0] == 1;
        }

        var exists = await inner.ExistsAsync(path);
        await cache.SetAsync(key, [exists ? (byte)1 : (byte)0], new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = ttl
        });
        return exists;
    }
}
