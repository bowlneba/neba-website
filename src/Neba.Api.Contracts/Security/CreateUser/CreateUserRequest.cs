namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>Creates a new user account. The invitee sets their own password via an emailed token link.</summary>
public sealed record CreateUserRequest
{
    /// <summary>
    /// The fields required to create a new user account.
    /// </summary>
    public required CreateUserInput User { get; init; }
}