using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Uploads;

namespace Neba.Api.Uploads;

/// <summary>
/// Base for the logo upload endpoints. It sends the staged file, or a 422 with the resizer's errors.
/// </summary>
internal abstract class LogoUploadEndpoint<TRequest> : Endpoint<TRequest, UploadedFileResponse>
    where TRequest : notnull
{
    protected async Task SendResultAsync(ErrorOr<UploadedFileResponse> result, CancellationToken cancellationToken)
    {
        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
            return;
        }

        await Send.OkAsync(result.Value, cancellationToken);
    }
}
