using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Security.Emails;

internal sealed class ResetPasswordCodeEmail(string resetCode)
{
    public string ToHtmlBody()
    {
        var code = WebUtility.HtmlEncode(resetCode);
        return EmailLayout.Wrap($"""
            <h1 style="margin:0 0 20px;font-size:22px;color:#1a3a6e;font-weight:700;">Your password reset code</h1>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              We received a request to reset the password for your BowlNEBA account. Enter the code below to complete the process.
            </p>
            <div style="text-align:center;margin:36px 0 28px;">
              <div style="display:inline-block;background:#f0f4fb;border:2px solid #1a3a6e;border-radius:6px;padding:16px 44px;font-size:30px;font-weight:700;letter-spacing:8px;color:#1a3a6e;font-family:'Courier New',monospace;">
                {code}
              </div>
            </div>
            <p style="margin:0;font-size:15px;line-height:1.65;color:#444;">
              If you did not request a password reset, no action is needed &mdash; your account remains secure.
            </p>
            """);
    }
}