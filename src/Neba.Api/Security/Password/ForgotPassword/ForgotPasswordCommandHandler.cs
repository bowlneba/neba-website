using System.Security.Cryptography;
using System.Text;

using ErrorOr;

using Hangfire;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Security.Password.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IFusionCache cache,
    IBackgroundJobClient backgroundJobClient,
    ILogger<ForgotPasswordCommandHandler> logger)
    : ICommandHandler<ForgotPasswordCommand>
{
    // One link per account per window, so the form cannot be used to flood someone's mailbox.
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    // User agents come from the caller; cap them so a log line cannot be padded or forged.
    private const int MaxUserAgentLength = 256;

    // Always succeeds, whether or not the account exists. The caller must not be able to tell.
    public async Task<ErrorOr<Success>> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);

        if (user is null)
        {
            logger.LogResetRequestedForUnknownEmail(
                command.Email,
                HashEmail(command.Email),
                EmailDomain(command.Email),
                command.RequesterIp ?? "unknown",
                Sanitize(command.UserAgent));

            return Result.Success;
        }

        var cooldownKey = $"password-reset-cooldown:{user.Id}";

        if ((await cache.TryGetAsync<bool>(cooldownKey, token: cancellationToken)).HasValue)
        {
            return Result.Success;
        }

        await cache.SetAsync(cooldownKey, true, new FusionCacheEntryOptions(Cooldown), token: cancellationToken);

        // The job builds the token, so no token is written to Hangfire storage.
        backgroundJobClient.Enqueue<SendPasswordResetEmailJob>(
            job => job.SendAsync(user.Id.ToString(), CancellationToken.None));

        return Result.Success;
    }

    private static string HashEmail(string email)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant())))[..12];

    private static string EmailDomain(string email)
    {
        var at = email.LastIndexOf('@');
        return at >= 0 ? email[(at + 1)..].ToUpperInvariant() : "unknown";
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var clean = new string([.. value.Where(c => !char.IsControl(c))]);
        return clean.Length <= MaxUserAgentLength ? clean : clean[..MaxUserAgentLength];
    }
}

internal static partial class ForgotPasswordCommandHandlerLogMessages
{
    /// <summary>
    /// A reset was requested for an address with no account. One of these is a typo; many from one IP, or
    /// many addresses at one domain, is an enumeration attempt. The raw email is logged on purpose: it is the
    /// evidence needed to recognise and block a probe, so this parameter is deliberately not [PersonalData].
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Password reset requested for unknown email {Email} (hash {EmailHash}, domain {EmailDomain}) from {ClientIp} with user agent {UserAgent}")]
    public static partial void LogResetRequestedForUnknownEmail(
        this ILogger<ForgotPasswordCommandHandler> logger,
        string email,
        string emailHash,
        string emailDomain,
        string clientIp,
        string userAgent);
}
