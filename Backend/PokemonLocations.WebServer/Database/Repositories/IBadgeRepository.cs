namespace PokemonLocations.WebServer.Database.Repositories;

public interface IBadgeRepository {
    Task<IReadOnlyList<string>> GetForUserAsync(int userId);
    Task AddAsync(int userId, string badge);
    Task RemoveAsync(int userId, string badge);
}
