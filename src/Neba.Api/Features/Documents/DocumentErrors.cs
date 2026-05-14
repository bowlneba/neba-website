using ErrorOr;

namespace Neba.Api.Features.Documents;

internal static class DocumentErrors
{
    public static Error DocumentNotFound(string documentName)
        => Error.NotFound(
            code: "Document.NotFound",
            description: $"Document with name '{documentName}' was not found.");
}