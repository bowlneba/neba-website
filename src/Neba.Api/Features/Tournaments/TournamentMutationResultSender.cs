using ErrorOr;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Maps a failed tournament-sponsor command result onto the 409/422 HTTP responses shared by
/// <see cref="AddTournamentSponsor.AddTournamentSponsorEndpoint"/> and
/// <see cref="RemoveTournamentSponsor.RemoveTournamentSponsorEndpoint"/>. The 404-not-found branch is
/// handled separately by each endpoint. Takes the endpoint's own <c>AddError</c>/<c>Send.ErrorsAsync</c>
/// as delegates since FastEndpoints' response sender is only reachable from within an endpoint instance.
/// </summary>
internal static class TournamentMutationResultSender
{
    public static async Task SendConflictOrValidationErrorsAsync(
        Error firstError,
        IReadOnlyCollection<Error> errors,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken ct)
    {
        if (firstError.Type == ErrorType.Conflict)
        {
            addError(firstError.Description);
            await sendErrorsAsync(StatusCodes.Status409Conflict, ct);
            return;
        }

        foreach (var error in errors)
        {
            addError(error.Description);
        }

        await sendErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
    }
}
