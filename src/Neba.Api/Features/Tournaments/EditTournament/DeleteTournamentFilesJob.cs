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
    /// <remarks>
    /// Copied into a <see cref="List{T}"/> because Hangfire's JSON serializer records the runtime type,
    /// and it cannot rebuild the compiler-generated type a collection expression produces.
    /// </remarks>
    public required IReadOnlyCollection<TournamentFileReference> Files
    {
        get;
        init => field = value.ToList();
    }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteTournamentFilesJob)}: {Files.Count} file(s)";
}