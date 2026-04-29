using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Tests.Infrastructure;

public static class TestHelpers {
    public static AuthenticationHeaderValue BasicHeader(string email, string password) {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) {
        var raw = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(raw).RootElement;
    }

    public static async Task SeedUserAsync(
        string connectionString, string email, string password, string displayName) {
        var hasher = new PasswordHasher();
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new UserRepository(dataSource);
        await repository.CreateAsync(email, hasher.HashPassword(password), displayName);
    }

    public static async Task ResetUsersAsync(string connectionString) {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("DELETE FROM users");
    }
}
