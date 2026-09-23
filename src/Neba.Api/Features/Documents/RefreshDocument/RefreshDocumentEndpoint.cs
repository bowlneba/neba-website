using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Documents.RefreshDocument;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentEndpoint(Messaging.ICommandHandler<RefreshDocumentCommand, Deleted> commandHandler)
        : Endpoint<RefreshDocumentRequest>
{
    private readonly Messaging.ICommandHandler<RefreshDocumentCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{DocumentName}/cache");
        Group<DocumentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Documents")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.RefreshDocument.PolicyName);

        Description(description => description
            .WithName("RefreshDocument")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(RefreshDocumentRequest req, CancellationToken ct)
    {
        var command = new RefreshDocumentCommand { DocumentName = req.DocumentName };
        await _commandHandler.HandleAsync(command, ct);

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}