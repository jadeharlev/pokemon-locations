using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Database.Repositories;

public enum AddResult { Success, AtCap }

public interface IUserImageRepository {
    /// <summary>
    /// Inserts inside a SERIALIZABLE transaction with a cap re-check.
    /// Returns Success on insert; AtCap if user already has &gt;= cap images at the location.
    /// Throws Npgsql.PostgresException(SqlState="40001") on serialization conflict — the controller
    /// catches this and retries once (see UserImagesController).
    /// </summary>
    Task<AddResult> AddAsync(UserImage image, int locationCap);

    /// <summary>Newest-first by uploaded_at. Scoped to (user, location).</summary>
    Task<IReadOnlyList<UserImage>> GetForUserAndLocationAsync(int userId, int locationId);

    /// <summary>Returns the row when (imageId, userId) matches; null otherwise.</summary>
    Task<UserImage?> GetByIdForUserAsync(int userId, Guid imageId);

    /// <summary>Idempotent: deleting a missing row is success.</summary>
    Task RemoveAsync(int userId, Guid imageId);

    Task<int> CountForUserAndLocationAsync(int userId, int locationId);
}
