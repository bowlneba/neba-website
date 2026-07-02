using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Security.Emails;

internal sealed class AdminResetPasswordEmail(string tempPassword)
{
    public string ToHtmlBody()
    {
        var password = WebUtility.HtmlEncode(tempPassword);
        return EmailLayout.Wrap($"""
            <h1 style="margin:0 0 20px;font-size:22px;color:#1a3a6e;font-weight:700;">Your password has been reset</h1>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              An administrator has reset your BowlNEBA password.
            </p>
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" align="center" style="margin:36px auto 28px;">
              <tr>
                <td style="background:#f0f4fb;border:2px solid #1a3a6e;border-radius:6px;padding:16px 44px;">
                  <span style="font-size:22px;font-weight:700;letter-spacing:2px;color:#1a3a6e;font-family:'Courier New',monospace;">
                    {password}
                  </span>
                </td>
              </tr>
            </table>
            <p style="margin:0;font-size:15px;line-height:1.65;color:#444;">
              Please log in and change your password as soon as possible.
            </p>
            """);
    }
}