using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PokemonLocations.Api.Tests.Infrastructure;

public static class JwtTokenTestHelper {
    public const string Key = "pokemon-locations-test-signing-key-must-be-at-least-32-bytes!!";
    public const string Issuer = "pokemon-locations-api-test";
    public const string Audience = "pokemon-locations-clients-test";

    public static string Create(
        string subject = "test-client",
        string? issuer = null,
        string? audience = null,
        string? key = null,
        DateTime? expires = null
    ) {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = expires ?? DateTime.UtcNow.AddHours(1);
        var notBefore = expiresAt.AddHours(-2);

        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, subject)],
            notBefore: notBefore,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
