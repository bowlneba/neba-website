using System.Net.Mime;

using FastEndpoints;

namespace Neba.Api;

internal sealed class BaseEndpointGroup
    : Group
{
    public BaseEndpointGroup()
    {
        Configure(string.Empty,
            definition => definition.Description(
                description => description.Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(
                    StatusCodes.Status500InternalServerError,
                    MediaTypeNames.Application.ProblemJson)));
    }
}