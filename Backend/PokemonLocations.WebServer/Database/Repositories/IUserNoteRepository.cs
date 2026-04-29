namespace PokemonLocations.WebServer.Database.Repositories;

public interface IUserNoteRepository {
    Task<string?> GetAsync(int userId, int locationId);
    Task UpsertAsync(int userId, int locationId, string noteText);
    Task DeleteAsync(int userId, int locationId);
}
