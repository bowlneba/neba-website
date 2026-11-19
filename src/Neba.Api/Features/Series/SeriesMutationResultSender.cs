using ErrorOr;

using FluentValidation.Results;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesMutationResultSender
{
    public static Task SendAsync(
        IReadOnlyList<Error> errors,
        Action<ValidationFailure> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken ct)
    {
        var validation = errors.Where(e => e.Type != ErrorType.Conflict).ToList();
        var toSend = validation.Count > 0 ? validation : errors;

        foreach (var error in toSend)
        {
            addError(ToFailure(error));
        }

        return sendErrorsAsync(
            validation.Count > 0 ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status409Conflict, ct);
    }

    private static ValidationFailure ToFailure(Error error)
    {
        // Stage errors name their row; Finale errors name their field (PropertyKey); the rest belong to the Series.
        var property = "Series";

        if (error.Metadata is { } metadata)
        {
            if (metadata.TryGetValue(SeriesErrors.StageOrderKey, out var order) && order is int stageOrder)
            {
                property = $"Series.Stages[{stageOrder - 1}]";
            }
            else if (metadata.TryGetValue(SeriesErrors.PropertyKey, out var named) && named is string name)
            {
                property = name;
            }
        }

        return new ValidationFailure(property, error.Description) { ErrorCode = error.Code };
    }
}