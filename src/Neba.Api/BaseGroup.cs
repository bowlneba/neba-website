using System.Net.Mime;

using FastEndpoints;

namespace Neba.Api;

internal sealed class BaseGroup
    : Group
{
    public BaseGroup()
    {
        Configure(string.Empty,
            definition => definition.Description(
                description => description.Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(
                    StatusCodes.Status500InternalServerError,
                    MediaTypeNames.Application.ProblemJson)));
    }
}