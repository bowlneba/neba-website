using ErrorOr;

using FluentValidation.Results;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesAdvancementResultSender
{
    /// <summary>
    /// NotFound is 404. A confirmation request is 409 with one failure per official Stage ("Series.Stages[n]"), so a client
    /// can list them. Everything else goes through <see cref="SeriesMutationResultSender"/> (409 for conflicts, 422 for the rest).
    /// </summary>
    public static Task SendAsync(
        IReadOnlyList<Error> errors,
        Action<ValidationFailure> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        Func<CancellationToken, Task> sendNotFoundAsync,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(addError);
        ArgumentNullException.ThrowIfNull(sendErrorsAsync);
        ArgumentNullException.ThrowIfNull(sendNotFoundAsync);

        var first = errors[0];

        if (first.Type == ErrorType.NotFound)
        {
            return sendNotFoundAsync(ct);
        }

        if (first.Metadata is { } metadata
            && metadata.TryGetValue(SeriesAdvancementErrors.OfficialStageOrdersKey, out var value)
            && value is int[] officialOrders)
        {
            foreach (var order in officialOrders)
            {
                addError(new ValidationFailure($"Series.Stages[{order - 1}]", first.Description) { ErrorCode = first.Code });
            }

            return sendErrorsAsync(StatusCodes.Status409Conflict, ct);
        }

        return SeriesMutationResultSender.SendAsync(errors, addError, sendErrorsAsync, ct);
    }
}