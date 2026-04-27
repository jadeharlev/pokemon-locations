using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PokemonLocations.WebServer.Clients;

public class JwtTokenProvider : IJwtTokenProvider {
    private readonly string key;
    private readonly string issuer;
    private readonly string audience;
    private readonly object refreshLock = new();
    private string currentToken;

    public JwtTokenProvider(string key, string issuer, string audience) {
        this.key = key;
        this.issuer = issuer;
        this.audience = audience;
        currentToken = Mint();
    }

    public string GetCurrentToken() => currentToken;

    public void Refresh() {
        lock (refreshLock) {
            currentToken = Mint();
        }
    }

    private string Mint() {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "web-server")],
            notBefore: now,
            expires: now.AddHours(24),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
