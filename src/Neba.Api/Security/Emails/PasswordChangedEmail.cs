using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Security.Emails;

internal sealed class PasswordChangedEmail(string websiteBaseUrl)
{
    public string ToHtmlBody()
    {
        var forgotLink = WebUtility.HtmlEncode($"{websiteBaseUrl}/account/forgot-password");
        return EmailLayout.Wrap($"""
            <h1 style="margin:0 0 20px;font-size:22px;color:#1a3a6e;font-weight:700;">Your password was changed</h1>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              The password for your BowlNEBA account was just changed.
            </p>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              If this was you, no action is needed. If it was not, <a href="{forgotLink}" style="color:#1a3a6e;">reset your password</a>
              right away and contact BowlNEBA support (website@bowlneba.com).
            </p>
            """, websiteBaseUrl);
    }
}