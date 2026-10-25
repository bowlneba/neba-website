using System.ComponentModel.DataAnnotations;

namespace Neba.Website.Server.Account.ForgotPassword;

// Properties use `set` rather than the project-wide `init` convention because EditForm binding
// needs mutable setters (same as LoginFormModel).
internal sealed record ForgotPasswordFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
