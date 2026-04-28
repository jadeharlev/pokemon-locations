using System.Security.Claims;
using NSubstitute;
using PokemonLocations.WebServer.Authentication;
using PokemonLocations.WebServer.Database.Repositories;
using PokemonLocations.WebServer.Models;

namespace PokemonLocations.WebServer.Tests.Authentication;

public class BasicAuthCredentialValidatorTests {
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly PasswordHasher passwordHasher = new();

    [Fact]
    public async Task ValidateAsyncReturnsSuccessForMatchingCredentials() {
        var hash = passwordHasher.HashPassword("pikachu123");
        var user = new User {
            UserId = 7,
            Email = "red@example.com",
            PasswordHash = hash,
            DisplayName = "Red",
            Theme = "bulbasaur"
        };
        userRepository.GetByEmailAsync("red@example.com").Returns(user);
        var validator = new BasicAuthCredentialValidator(userRepository, passwordHasher);

        var result = await validator.ValidateAsync("red@example.com", "pikachu123");

        Assert.True(result.Success);
        Assert.NotNull(result.Principal);
        Assert.Equal("7", result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Red", result.Principal!.FindFirstValue(ClaimTypes.Name));
    }

    [Fact]
    public async Task ValidateAsyncReturnsFailureForWrongPassword() {
        var hash = passwordHasher.HashPassword("pikachu123");
        var user = new User {
            UserId = 7, Email = "red@example.com", PasswordHash = hash,
            DisplayName = "Red", Theme = "bulbasaur"
        };
        userRepository.GetByEmailAsync("red@example.com").Returns(user);
        var validator = new BasicAuthCredentialValidator(userRepository, passwordHasher);

        var result = await validator.ValidateAsync("red@example.com", "wrong-password");

        Assert.False(result.Success);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task ValidateAsyncReturnsFailureForUnknownEmail() {
        userRepository.GetByEmailAsync("nobody@example.com").Returns((User?)null);
        var validator = new BasicAuthCredentialValidator(userRepository, passwordHasher);

        var result = await validator.ValidateAsync("nobody@example.com", "anything");

        Assert.False(result.Success);
        Assert.Null(result.Principal);
    }
}
