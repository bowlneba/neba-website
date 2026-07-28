namespace Neba.Api.Security.CreateUser;

internal sealed record CreateUserResult(Ulid UserId, bool RolesAssigned);