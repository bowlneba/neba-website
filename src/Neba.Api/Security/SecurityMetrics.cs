using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Neba.Api.Security;

internal static class SecurityMetrics
{
    private const string ResultTagName = "result";

    internal const string MeterName = "Neba.Security";
    internal const string ResetRequestCountName = "neba.security.password_reset.request.count";
    internal const string PasswordChangeCountName = "neba.security.password_change.count";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> ResetRequestCount
        = Meter.CreateCounter<long>(
            ResetRequestCountName,
            description: "Counts password reset requests by outcome (sent, cooldown, unknown_email).");

    private static readonly Counter<long> PasswordChangeCount
        = Meter.CreateCounter<long>(
            PasswordChangeCountName,
            description: "Counts change-password attempts by outcome.");

    public static void RecordResetRequest(string result)
        => ResetRequestCount.Add(1, new TagList { { ResultTagName, result } });

    public static void RecordPasswordChange(string result)
        => PasswordChangeCount.Add(1, new TagList { { ResultTagName, result } });
}
