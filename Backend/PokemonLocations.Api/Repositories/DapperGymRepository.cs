using Dapper;
using Npgsql;
using PokemonLocations.Api.Data.Models;

namespace PokemonLocations.Api.Repositories;

public class DapperGymRepository : IGymRepository {
    private readonly NpgsqlDataSource dataSource;

    private const string SelectGymSummary = @"
        SELECT g.gym_id,
               g.building_id,
               b.location_id,
               l.name AS location_name,
               b.name AS building_name,
               g.gym_type,
               g.badge_name,
               g.gym_leader,
               g.gym_order
          FROM gyms g
          JOIN buildings b ON b.building_id = g.building_id
          JOIN locations l ON l.location_id = b.location_id";

    public DapperGymRepository(NpgsqlDataSource dataSource) {
        this.dataSource = dataSource;
    }

    public async Task<IEnumerable<GymSummary>> GetAllAsync() {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryAsync<GymSummary>(
            SelectGymSummary + " ORDER BY g.gym_order");
    }

    public async Task<GymSummary?> GetByIdAsync(int gymId) {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<GymSummary>(
            SelectGymSummary + " WHERE g.gym_id = @GymId",
            new { GymId = gymId });
    }
}
