using System.ComponentModel.DataAnnotations;

namespace PokemonLocations.WebServer.Models.Requests;

public class SignupRequest : IValidatableObject {
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (string.IsNullOrWhiteSpace(DisplayName)) {
            yield return new ValidationResult(
                "DisplayName must not be empty or whitespace.",
                [nameof(DisplayName)]);
        }
    }
}
