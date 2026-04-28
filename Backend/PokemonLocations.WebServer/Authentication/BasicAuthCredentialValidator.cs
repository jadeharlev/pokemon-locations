using System.Security.Claims;
using idunno.Authentication.Basic;
using PokemonLocations.WebServer.Database.Repositories;

namespace PokemonLocations.WebServer.Authentication;

public class BasicAuthCredentialValidator {
    private readonly IUserRepository userRepository;
    private readonly PasswordHasher passwordHasher;

    public BasicAuthCredentialValidator(IUserRepository userRepository, PasswordHasher passwordHasher) {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
    }

    public async Task<ValidationResult> ValidateAsync(string username, string password) {
        var user = await userRepository.GetByEmailAsync(username);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash)) {
            return new ValidationResult(false, null);
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName)
            ],
            BasicAuthenticationDefaults.AuthenticationScheme);

        return new ValidationResult(true, new ClaimsPrincipal(identity));
    }

    public record ValidationResult(bool Success, ClaimsPrincipal? Principal);
}
