using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.ForgotPassword;

internal sealed record ForgotPasswordCommand
    : ICommand
{
    public required string Email { get; init; }

    /// <summary>
    /// The visitor's IP, filled by the endpoint. Used only for the unknown-email log.
    /// </summary>
    public string? RequesterIp { get; init; }

    /// <summary>
    /// The visitor's user agent, filled by the endpoint. Used only for the unknown-email log.
    /// </summary>
    public string? UserAgent { get; init; }
}
