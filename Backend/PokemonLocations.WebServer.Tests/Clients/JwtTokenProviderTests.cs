using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PokemonLocations.WebServer.Clients;

namespace PokemonLocations.WebServer.Tests.Clients;

public class JwtTokenProviderTests {
    private const string Key = "pokemon-locations-webserver-test-key-must-be-32-bytes-or-more!";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";

    [Fact]
    public void GetCurrentTokenReturnsTokenWithSubjectWebServer() {
        var provider = new JwtTokenProvider(Key, Issuer, Audience);

        var token = provider.GetCurrentToken();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("web-server", jwt.Subject);
    }

    [Fact]
    public void GetCurrentTokenReturnsTokenWithConfiguredIssuerAndAudience() {
        var provider = new JwtTokenProvider(Key, Issuer, Audience);

        var token = provider.GetCurrentToken();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void GetCurrentTokenReturnsTokenExpiringInApproximately24Hours() {
        var before = DateTime.UtcNow;
        var provider = new JwtTokenProvider(Key, Issuer, Audience);
        var after = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(provider.GetCurrentToken());

        Assert.InRange(
            jwt.ValidTo,
            before.AddHours(24).AddMinutes(-1),
            after.AddHours(24).AddMinutes(1));
    }

    [Fact]
    public void GetCurrentTokenReturnsTokenThatValidatesAgainstSameKey() {
        var provider = new JwtTokenProvider(Key, Issuer, Audience);
        var token = provider.GetCurrentToken();

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ClockSkew = TimeSpan.Zero
        };

        handler.ValidateToken(token, parameters, out _);
    }

    [Fact]
    public void RefreshProducesNewTokenStringStillValid() {
        var provider = new JwtTokenProvider(Key, Issuer, Audience);
        var first = provider.GetCurrentToken();

        Thread.Sleep(1100);
        provider.Refresh();
        var second = provider.GetCurrentToken();

        Assert.NotEqual(first, second);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(second);
        Assert.Equal("web-server", jwt.Subject);
    }
}
