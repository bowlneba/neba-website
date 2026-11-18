using System.ComponentModel.DataAnnotations;

using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Website.Server.Series;

internal sealed class SeriesFormModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(127, ErrorMessage = "Name must be 127 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Set on create; read-only on edit.</summary>
    [Required(ErrorMessage = "Season is required.")]
    public string? SeasonId { get; set; }

    /// <summary>The logo to save: the stored one (edit), a newly staged one, or null for none.</summary>
    public SeriesLogoInput? Logo { get; set; }

    /// <summary>Preview URL for <see cref="Logo"/>.</summary>
    public Uri? LogoUrl { get; set; }

    public List<SeriesStageRowModel> Stages { get; } = [];

    /// <summary>Null means the Series has no Finale. The editor's "This Series has a Finale" toggle sets or clears it.</summary>
    public SeriesFinaleFormModel? Finale { get; set; }
}

internal sealed class SeriesFinaleFormModel
{
    /// <summary>Bowlers from the Placement Round who go to the Finale. Required and above zero.</summary>
    [Required(ErrorMessage = "Number of finalists is required.")]
    public int? FinalistCount { get; set; }

    [Required(ErrorMessage = "Date is required.")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly? StartTime { get; set; }

    /// <summary>Certification number of the host center; null while it is to be announced.</summary>
    public string? BowlingCenterId { get; set; }

    /// <summary>Server errors named <c>Series.Finale.*</c>.</summary>
    public List<string> Errors { get; } = [];
}
