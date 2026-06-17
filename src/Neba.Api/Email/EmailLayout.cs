using System.Reflection;

namespace Neba.Api.Email;

internal static class EmailLayout
{
    private static readonly Lazy<string> _logoBase64 = new(LoadLogoBase64);

    private static string LoadLogoBase64()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Neba.Api.Email.Resources.neba-logo.png")!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return Convert.ToBase64String(bytes);
    }

    internal static string Wrap(string innerHtml)
    {
        var year = DateTime.UtcNow.Year;
        var logo = _logoBase64.Value;
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin:0;padding:40px 20px;background:#e8e8e8;font-family:Arial,Helvetica,sans-serif;">
              <div style="max-width:580px;margin:0 auto;border-radius:6px;overflow:hidden;">
                <div style="background:#1a3a6e;padding:28px 40px;text-align:center;">
                  <img src="data:image/png;base64,{logo}" alt="New England Bowlers Association" style="height:96px;width:auto;display:inline-block;" />
                </div>
                <div style="background:#ffffff;padding:44px 52px 36px;">
                  {innerHtml}
                </div>
                <div style="background:#f4f4f4;border-top:1px solid #ddd;padding:20px 52px;text-align:center;">
                  <p style="margin:4px 0;font-size:12px;color:#999;">&copy; {year} New England Bowlers Association</p>
                  <p style="margin:4px 0;font-size:12px;color:#999;">
                    <a href="https://bowlneba.com" style="color:#1a3a6e;text-decoration:none;">bowlneba.com</a>
                    &nbsp;&nbsp;&bull;&nbsp;&nbsp;
                    <a href="mailto:website@bowlneba.com" style="color:#1a3a6e;text-decoration:none;">website@bowlneba.com</a>
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;
    }
}
