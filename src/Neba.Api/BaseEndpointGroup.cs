using System.Net.Mime;

using FastEndpoints;

using Neba.Api.RateLimiting;

namespace Neba.Api;

internal sealed class BaseEndpointGroup
    : Group
{
    public BaseEndpointGroup()
    {
        Configure(string.Empty, definition =>
        {
            definition.Options(options => options.RequireRateLimiting(RateLimitingConfiguration.PublicPolicy));
            definition.Description(description => description
                .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(
                    StatusCodes.Status429TooManyRequests,
                    MediaTypeNames.Application.ProblemJson)
                .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(
                    StatusCodes.Status500InternalServerError,
                    MediaTypeNames.Application.ProblemJson));
        });
    }
}