namespace PokemonLocations.Api.Repositories;

public interface IDatabaseHealthRepository {
    Task<bool> GetHealth();
}