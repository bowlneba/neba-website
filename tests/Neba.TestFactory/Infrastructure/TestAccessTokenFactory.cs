using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using Neba.Api.Contracts.Security;
using Neba.Api.Security;

namespace Neba.TestFactory.Infrastructure;

/// <summary>
/// Builds signed JWTs matching the shape <c>JwtTokenService</c> issues in production, for
/// integration tests that spin up a real host and need to exercise the actual
/// authentication/authorization pipeline (401/403 behavior) rather than mocking it.
/// </summary>
public static class TestAccessTokenFactory
{
    /// <summary>
    /// JwtSettings a test host's configuration must be seeded with so tokens created by
    /// <see cref="Create"/> validate against that host's JWT bearer options.
    /// </summary>
    public static readonly JwtSettings Settings = new()
    {
        Issuer = "https://bowlneba.com",
        Audience = "https://bowlneba.com",
        SigningKey = new string('a', 32),
    };

    public static string Create(
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<Permissions>? permissions = null)
    {
        List<Claim> claims = [new Claim(JwtRegisteredClaimNames.Sub, Ulid.NewUlid().ToString())];

        foreach (var role in roles ?? [])
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions ?? [])
        {
            claims.Add(new Claim(Permissions.ClaimType, permission.Value));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: Settings.Issuer,
            audience: Settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}