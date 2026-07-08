using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.News.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleEndpoint(Messaging.ICommandHandler<DeleteArticleCommand, Deleted> commandHandler)
    : Endpoint<DeleteArticleRequest>
{
    private readonly Messaging.ICommandHandler<DeleteArticleCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}");
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.DeleteArticle.PolicyName);

        Description(description => description
            .WithName("DeleteArticle")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(DeleteArticleRequest req, CancellationToken ct)
    {
        var command = new DeleteArticleCommand { ArticleId = new ArticleId(req.Id) };
        await _commandHandler.HandleAsync(command, ct);

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
