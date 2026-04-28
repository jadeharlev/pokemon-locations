using PokemonLocations.WebServer.Authentication;

namespace PokemonLocations.WebServer.Tests.Authentication;

public class PasswordHasherTests {
    private readonly PasswordHasher hasher = new();

    [Fact]
    public void HashPasswordReturnsValueDifferentFromInput() {
        var hash = hasher.HashPassword("correct horse battery staple");

        Assert.NotEqual("correct horse battery staple", hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPasswordProducesDifferentHashesForSameInput() {
        var first = hasher.HashPassword("correct horse battery staple");
        var second = hasher.HashPassword("correct horse battery staple");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void VerifyReturnsTrueForMatchingPassword() {
        var hash = hasher.HashPassword("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void VerifyReturnsFalseForNonMatchingPassword() {
        var hash = hasher.HashPassword("correct horse battery staple");

        Assert.False(hasher.Verify("wrong password", hash));
    }
}
