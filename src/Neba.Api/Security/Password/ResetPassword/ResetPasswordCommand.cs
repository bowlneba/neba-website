using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed record ResetPasswordCommand
    : ICommand
{
    public required Ulid UserId { get; init; }
}