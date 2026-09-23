using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed record RefreshDocumentCommand : ICommand<Deleted>
{
    public required string DocumentName { get; init; }
}
