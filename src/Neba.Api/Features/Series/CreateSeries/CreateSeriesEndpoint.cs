using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.CreateSeries;

internal sealed class CreateSeriesEndpoint(Messaging.ICommandHandler<CreateSeriesCommand, SeriesId> commandHandler)
    : Endpoint<CreateSeriesRequest, CreatedSeriesResponse>
{
    private readonly Messaging.ICommandHandler<CreateSeriesCommand, SeriesId> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateSeries.PolicyName);

        Description(description => description
            .WithName("CreateSeries")
            .WithTags("Admin")
            .Produces<CreatedSeriesResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateSeriesRequest req, CancellationToken ct)
    {
        var command = new CreateSeriesCommand
        {
            Name = req.Series.Name,
            SeasonId = new SeasonId(req.Series.SeasonId),
            Logo = SeriesStageInputMapper.ToLogo(req.Series.Logo),
            Stages = SeriesStageInputMapper.ToDefinitions(req.Series.Stages),
            Finale = SeriesStageInputMapper.ToFinale(req.Series.Finale),
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            await SeriesMutationResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        var id = result.Value.Value.ToString();

        // Stryker disable once Statement
        await Send.CreatedAtAsync("GetSeries", routeValues: new { id }, responseBody: new CreatedSeriesResponse { SeriesId = id }, cancellation: ct);
    }
}
