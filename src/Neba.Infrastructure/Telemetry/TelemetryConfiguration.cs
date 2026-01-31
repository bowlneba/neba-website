using System.Diagnostics;

namespace Neba.Infrastructure.Telemetry;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should be static

internal static class TelemetryConfiguration
{
    extension(Activity? activity)
    {
        /// <summary>
        /// Sets code-related attributes on the activity.
        /// </summary>
        /// <param name="function">The function name.</param>
        /// <param name="namespace">The namespace.</param>
        /// <returns>The activity with the attributes set, or null if the activity is null.</returns>
        public Activity? SetCodeAttributes(
            string function,
            string @namespace)
        {
            if (activity is null)
            {
                return null;
            }

            activity.SetTag("code.function", function);

            if (!string.IsNullOrEmpty(@namespace))
            {
                activity.SetTag("code.namespace", @namespace);
            }

            return activity;
        }

        public Activity? SetExceptionTags(Exception ex)
        {
            if (activity is null)
            {
                return null;
            }

            activity.SetTag("error.type", ex.GetErrorType());
            activity.SetTag("error.message", ex.Message);
            activity.SetTag("error.stack_trace", ex.StackTrace);
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);

            return activity;
        }
    }

    extension(Exception ex)
    {
        private string GetErrorType()
            => ex.GetType().FullName ?? ex.GetType().Name;
    }
}