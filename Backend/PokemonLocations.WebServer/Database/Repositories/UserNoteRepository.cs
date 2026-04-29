using Dapper;
using Npgsql;

namespace PokemonLocations.WebServer.Database.Repositories;

public class UserNoteRepository : IUserNoteRepository {
    private readonly NpgsqlDataSource dataSource;

    public UserNoteRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<string?> GetAsync(int userId, int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<string?>(
            @"SELECT note_text
                FROM user_location_notes
               WHERE user_id = @UserId
                 AND location_id = @LocationId",
            new { UserId = userId, LocationId = locationId });
    }

    public async Task UpsertAsync(int userId, int locationId, string noteText) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO user_location_notes (user_id, location_id, note_text, updated_at)
              VALUES (@UserId, @LocationId, @NoteText, now())
              ON CONFLICT (user_id, location_id) DO UPDATE
                 SET note_text = EXCLUDED.note_text,
                     updated_at = now()",
            new { UserId = userId, LocationId = locationId, NoteText = noteText });
    }

    public async Task DeleteAsync(int userId, int locationId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            @"DELETE FROM user_location_notes
               WHERE user_id = @UserId
                 AND location_id = @LocationId",
            new { UserId = userId, LocationId = locationId });
    }
}
