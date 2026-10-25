using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed record ChangePasswordCommand
    : ICommand<ChangePasswordDto>
{
    public required Ulid UserId { get; init; }

    public required string CurrentPassword { get; init; }

    public required string NewPassword { get; init; }
}
