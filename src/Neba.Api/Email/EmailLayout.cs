namespace Neba.Api.Email;

internal static class EmailLayout
{
    internal static string Wrap(string innerHtml)
    {
        var year = DateTime.UtcNow.Year;
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin:0;padding:0;background:#e8e8e8;font-family:Arial,Helvetica,sans-serif;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#e8e8e8;font-family:Arial,Helvetica,sans-serif;">
                <tr>
                  <td align="center" style="padding:40px 20px;">
                    <table role="presentation" width="580" cellspacing="0" cellpadding="0" border="0" style="max-width:580px;width:100%;">
                      <tr>
                        <td style="background:#1a3a6e;padding:28px 40px;text-align:center;">
                          <img src="https://bowlneba.com/images/neba-logo.png" alt="New England Bowlers Association" height="96" style="display:block;margin:0 auto;width:auto;" />
                        </td>
                      </tr>
                      <tr>
                        <td style="background:#ffffff;padding:44px 52px 36px;">
                          {innerHtml}
                        </td>
                      </tr>
                      <tr>
                        <td style="background:#f4f4f4;border-top:1px solid #ddd;padding:20px 52px;text-align:center;">
                          <p style="margin:4px 0;font-size:12px;color:#999;">&copy; {year} New England Bowlers Association</p>
                          <p style="margin:4px 0;font-size:12px;color:#999;">
                            <a href="https://bowlneba.com" style="color:#1a3a6e;text-decoration:none;">bowlneba.com</a>
                            &nbsp;&nbsp;&bull;&nbsp;&nbsp;
                            <a href="mailto:website@bowlneba.com" style="color:#1a3a6e;text-decoration:none;">website@bowlneba.com</a>
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
