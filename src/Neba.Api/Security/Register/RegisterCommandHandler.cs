using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Register;

internal sealed class RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<RegisterCommand, Ulid>
{
    public async Task<ErrorOr<Ulid>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, command.Password);

        if (result.Succeeded)
        {
            return user.Id;
        }

        var isDuplicate = result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");

        return isDuplicate
            ? RegisterErrors.DuplicateEmail
            : result.Errors
            .Select(error => Error.Validation(error.Code, error.Description))
            .ToList();
    }
}