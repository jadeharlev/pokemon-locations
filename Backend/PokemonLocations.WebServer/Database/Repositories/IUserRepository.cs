using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Database.Repositories;

public interface IUserRepository {
    Task<User> CreateAsync(string email, string passwordHash, string displayName);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int userId);
    Task DeleteAsync(int userId);
}
