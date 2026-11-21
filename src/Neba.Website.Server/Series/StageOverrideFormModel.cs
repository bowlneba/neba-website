using System.ComponentModel.DataAnnotations;

namespace Neba.Website.Server.Series;

/// <summary>The override dialog's form. The bowler picker is not bound through the EditContext.</summary>
public sealed class StageOverrideFormModel
{
    /// <summary>Most characters a reason may have.</summary>
    public const int ReasonMaxLength = 500;

    /// <summary>Why the admin made the change.</summary>
    [Required(ErrorMessage = "A reason is required.")]
    [StringLength(ReasonMaxLength, ErrorMessage = "The reason must be 500 characters or fewer.")]
    public string Reason { get; set; } = string.Empty;
}