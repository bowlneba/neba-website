using System.ComponentModel.DataAnnotations;

using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Website.Server.Series;

/// <summary>The create and edit form for a Series.</summary>
public sealed class SeriesFormModel
{
    /// <summary>The Series name.</summary>
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

    /// <summary>The Stage rows, in order.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification="A mutable form model the editor binds to.")]
    public List<SeriesStageRowModel> Stages { get; } = [];

    /// <summary>Null means the Series has no Finale. The editor's "This Series has a Finale" toggle sets or clears it.</summary>
    public SeriesFinaleFormModel? Finale { get; set; }
}

/// <summary>The Finale part of the Series form.</summary>
public sealed class SeriesFinaleFormModel
{
    /// <summary>Bowlers from the Placement Round who go to the Finale. Required and above zero.</summary>
    [Required(ErrorMessage = "Number of finalists is required.")]
    public int? FinalistCount { get; set; }

    /// <summary>The Finale date.</summary>
    [Required(ErrorMessage = "Date is required.")]
    public DateOnly? Date { get; set; }

    /// <summary>The Finale start time.</summary>
    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly? StartTime { get; set; }

    /// <summary>Certification number of the host center; null while it is to be announced.</summary>
    public string? BowlingCenterId { get; set; }

    /// <summary>Server errors named <c>Series.Finale.*</c>.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification="A mutable form model the editor binds to.")]
    public List<string> Errors { get; } = [];
}
