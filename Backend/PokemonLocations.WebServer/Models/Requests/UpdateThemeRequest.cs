using System.ComponentModel.DataAnnotations;

namespace PokemonLocations.WebServer.Models.Requests;

public class UpdateThemeRequest : IValidatableObject {
    [Required]
    public string Theme { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (!Themes.IsValid(Theme)) {
            yield return new ValidationResult(
                $"Unknown theme: {Theme}",
                [nameof(Theme)]);
        }
    }
}
