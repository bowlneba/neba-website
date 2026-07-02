using System.ComponentModel.DataAnnotations;

namespace Neba.Website.Server.Account.Login;

// Properties use `set` rather than the project-wide `init` convention because this model is
// bound via [SupplyParameterFromForm], which requires mutable setters for Blazor's form binder.
internal sealed record LoginFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}