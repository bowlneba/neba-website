using Neba.Api.Contracts.Security.GetCurrentUser;
using Neba.Api.Contracts.Security.Login;
using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Api.Contracts.Security.Register;
using Neba.Api.Contracts.Security.ResetPassword;

using Refit;

namespace Neba.Api.Contracts.Security;

/// <summary>Defines the Security API contract for authentication and account management.</summary>
public interface ISecurityApi
{
    /// <summary>Registers a new user account.</summary>
    [Post("/security/register")]
    Task<IApiResponse<RegisterResponse>> RegisterAsync([Body] RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Authenticates with email and password, returning a JWT and refresh token.</summary>
    [Post("/security/login")]
    Task<IApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exchanges a valid refresh token for a new token pair.</summary>
    [Post("/security/refresh")]
    Task<IApiResponse<RefreshTokenResponse>> RefreshTokenAsync([Body] RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes the current user's refresh token.</summary>
    [Post("/security/logout")]
    Task<IApiResponse> LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the current authenticated user's profile.</summary>
    [Get("/security/me")]
    Task<IApiResponse<MeResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets any user's password directly (Admin only). No current password or email token required.</summary>
    [Post("/security/password/reset")]
    Task<IApiResponse> ResetPasswordAsync([Body] ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
