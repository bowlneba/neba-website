using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Documents.GetDocument;
using Neba.Application.Documents.GetDocument;
using Neba.Application.Messaging;

namespace Neba.Api.Documents.GetDocument;

internal sealed class GetDocumentEndpoint(IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>> queryHandler)
        : Endpoint<GetDocumentRequest, GetDocumentResponse>
{
    private readonly IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{DocumentName}");
        Group<DocumentsGroup>();

        Options(options => options
            .WithVersionSet("Documents")
            .MapToApiVersion(new ApiVersion(1, 0)));

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
            await Send.NotFoundAsync(ct);

            return;
        }

        var response = new GetDocumentResponse
        {
            Html = result.Value.Html,
            CachedAt = result.Value.CachedAt,
        };

        await Send.OkAsync(response, ct);
    }
}
