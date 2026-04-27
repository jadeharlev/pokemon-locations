using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PokemonLocations.TokenIssuer.Tests;

public class TokenBuilderTests {
    private const string TestKey = "pokemon-locations-issuer-test-key-must-be-32-bytes-or-more!";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    [Fact]
    public void BuildSetsSubjectClaimToClientArgument() {
        var token = TokenBuilder.Build("team-alpha", days: 30, TestKey, TestIssuer, TestAudience);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("team-alpha", jwt.Subject);
    }

    [Fact]
    public void BuildSetsIssuerAndAudienceClaims() {
        var token = TokenBuilder.Build("team-alpha", days: 30, TestKey, TestIssuer, TestAudience);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(TestIssuer, jwt.Issuer);
        Assert.Contains(TestAudience, jwt.Audiences);
    }

    [Fact]
    public void BuildSetsExpiryToNowPlusDaysArgument() {
        var before = DateTime.UtcNow;
        var token = TokenBuilder.Build("team-alpha", days: 30, TestKey, TestIssuer, TestAudience);
        var after = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.InRange(
            jwt.ValidTo,
            before.AddDays(30).AddSeconds(-5),
            after.AddDays(30).AddSeconds(5));
    }

    [Fact]
    public void BuildProducesTokenThatValidatesAgainstSameKey() {
        var token = TokenBuilder.Build("team-alpha", days: 30, TestKey, TestIssuer, TestAudience);

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
            ClockSkew = TimeSpan.Zero
        };

        handler.ValidateToken(token, parameters, out _);
    }
}
