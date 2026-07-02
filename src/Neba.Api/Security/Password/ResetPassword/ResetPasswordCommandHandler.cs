using System.Security.Cryptography;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Email;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

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

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, tempPassword);
        if (!resetResult.Succeeded)
        {
            return resetResult.Errors
                .Select(error => Error.Validation($"ResetPassword.{error.Code}", error.Description))
                .ToList();
        }

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Your BowlNEBA password has been reset",
            HtmlBody = new AdminResetPasswordEmail(tempPassword).ToHtmlBody()
        }, cancellationToken);

        return Result.Success;
    }

    // Guarantees at least one uppercase letter, one lowercase letter, and one digit
    // (Identity's RequireUppercase/RequireLowercase/RequireDigit policies), then shuffles
    // so the guaranteed characters aren't always in fixed positions.
    private static string GenerateTempPassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string all = uppercase + lowercase + digits;

        Span<char> password = stackalloc char[16];
        password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

        for (var i = 3; i < password.Length; i++)
        {
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (var i = password.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}