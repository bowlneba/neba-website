using FastEndpoints;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Neba.Api.ErrorHandling;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should not be static

internal static class ErrorHandlingConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures ProblemDetails for exception handling.
        /// Adds traceId and requestPath to all ProblemDetails responses.
        /// </summary>
        public IServiceCollection AddErrorHandling()
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    context.ProblemDetails.Extensions["requestPath"] = context.HttpContext.Request.Path.Value;
                };
            });

            return services;
        }
    }

    extension(ErrorOptions options)
    {
        /// <summary>
        /// Configures FastEndpoints error handling with RFC 7807 compliant ProblemDetails.
        /// </summary>
        public ErrorOptions ConfigureErrorHandling()
        {
            options.UseProblemDetails(problemDetailsOptions =>
            {
                problemDetailsOptions.AllowDuplicateErrors = true;
                problemDetailsOptions.IndicateErrorCode = true;
                problemDetailsOptions.IndicateErrorSeverity = true;

                problemDetailsOptions.TypeValue = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1";

                problemDetailsOptions.TitleTransformer = problemDetails => problemDetails.Status switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Not Found",
                    409 => "Conflict",
                    500 => "Internal Server Error",
                    _ => problemDetails.Title
                };
            });

            options.StatusCode = 400;
            options.ProducesMetadataType = typeof(ProblemDetails);

            return options;
        }
    }
}
