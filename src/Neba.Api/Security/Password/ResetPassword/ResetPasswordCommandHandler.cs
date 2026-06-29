using System.Security.Cryptography;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Email;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender)
        : ICommandHandler<ResetPasswordCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return ResetPasswordErrors.UserNotFound;
        }

        var tempPassword = GenerateTempPassword();

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return removeResult.Errors
                .Select(error => Error.Failure(error.Code, error.Description))
                .ToList();
        }

        var addResult = await userManager.AddPasswordAsync(user, tempPassword);
        if (!addResult.Succeeded)
        {
            return addResult.Errors
                .Select(error => Error.Failure(error.Code, error.Description))
                .ToList();
        }

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Your BowlNEBA password has been reset",
            HtmlBody =
$"""
<p>An administrator has reset your BowlNEBA password.</p>
<p>Your new temporary password is: <strong>{tempPassword}</strong></p>
<p>Please log in and change your password as soon as possible.</p>
"""
        }, cancellationToken);

        return Result.Success;
    }

    // 16-char base64 string (letters, digits, +, /, =).
    // Last character replaced with a decimal digit to guarantee Identity's RequireDigit policy.
    private static string GenerateTempPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        var b64 = Convert.ToBase64String(bytes);
        return b64[..15] + (char)('0' + (bytes[11] % 10));
    }
}