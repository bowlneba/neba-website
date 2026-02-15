using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Documents.GetDocument;
using Neba.Application.Documents.GetDocument;
using Neba.Application.Messaging;

namespace Neba.Api.Documents.GetDocument;

internal sealed class GetDocumentEndpoint(IQueryHandler<GetDocumentQuery, ErrorOr<string>> queryHandler)
        : Endpoint<GetDocumentRequest, GetDocumentResponse>
{
    private readonly IQueryHandler<GetDocumentQuery, ErrorOr<string>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{DocumentName}");
        Group<DocumentsGroup>();

        AllowAnonymous();

        Description(description => description
            .WithName("GetDocument")
            .WithTags("Public"));
    }

    public override async Task HandleAsync(GetDocumentRequest req, CancellationToken ct)
    {
        var query = new GetDocumentQuery
        {
            DocumentName = req.DocumentName,
        };

        var result = await _queryHandler.HandleAsync(query, ct);

        if (result.IsError)
        {
            AddError(result.FirstError.Description, result.FirstError.Code);
            await Send.ErrorsAsync(statusCode: StatusCodes.Status404NotFound, ct);

            return;
        }

        var response = new GetDocumentResponse
        {
            Html = result.Value,
        };

        await Send.OkAsync(response, ct);
    }
}