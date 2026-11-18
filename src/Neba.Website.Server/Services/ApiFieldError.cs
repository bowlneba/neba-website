using ErrorOr;

namespace Neba.Website.Server.Services;

/// <summary>One named error from an API ProblemDetails body (FastEndpoints <c>errors[].name</c>).</summary>
internal sealed record ApiFieldError(string Name, string Reason);

internal static class ApiFieldErrorExtensions
{
    public const string MetadataKey = "FieldErrors";

    extension(Error error)
    {
        /// <summary>The named errors the API returned; empty when there were none.</summary>
        public IReadOnlyList<ApiFieldError> GetFieldErrors()
            => error.Metadata?.TryGetValue(MetadataKey, out var value) == true
                && value is IReadOnlyList<ApiFieldError> fieldErrors
                ? fieldErrors
                : [];
    }
}
