using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Security.Emails;

internal sealed class ResetPasswordLinkEmail(string resetLink)
{
    public string ToHtmlBody()
    {
        var link = WebUtility.HtmlEncode(resetLink);
        return EmailLayout.Wrap($"""
            <h1 style="margin:0 0 20px;font-size:22px;color:#1a3a6e;font-weight:700;">Reset your password</h1>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              We received a request to reset the password for your BowlNEBA account. Click the button below to choose a new password.
            </p>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              If you did not request a password reset, no action is needed &mdash; your account remains secure.
            </p>
            <div style="text-align:center;margin:36px 0 28px;">
              <a href="{link}" style="display:inline-block;background:#1a3a6e;color:#ffffff;text-decoration:none;padding:14px 40px;border-radius:4px;font-size:15px;font-weight:700;">
                Reset Password
              </a>
            </div>
            <hr style="border:none;border-top:1px solid #ececec;margin:28px 0;" />
            <p style="font-size:12px;color:#999;line-height:1.6;margin:0;">
              If the button above does not work, copy and paste this link into your browser:<br />
              <a href="{link}" style="color:#1a3a6e;">{link}</a>
            </p>
            """);
    }
}