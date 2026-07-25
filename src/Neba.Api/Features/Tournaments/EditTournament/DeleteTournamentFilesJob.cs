using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.Tournaments.EditTournament;

/// <summary>
/// Represents a background job that deletes files associated with a tournament.
/// </summary>
public sealed record DeleteTournamentFilesJob
    : IBackgroundJob
{
    /// <summary>
    /// Gets the collection of files to be deleted.
    /// </summary>
    public required IReadOnlyCollection<TournamentFileReference> Files { get; init; }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteTournamentFilesJob)}: {Files.Count} file(s)";
}