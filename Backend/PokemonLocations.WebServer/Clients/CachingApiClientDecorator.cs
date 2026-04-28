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
}
