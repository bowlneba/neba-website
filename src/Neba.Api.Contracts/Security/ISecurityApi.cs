using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Contracts.Security.GetCurrentUser;
using Neba.Api.Contracts.Security.ListUsers;
using Neba.Api.Contracts.Security.Login;
using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Api.Contracts.Security.ResetPassword;
using Neba.Api.Contracts.Security.SetPasswordFromToken;

using Refit;

namespace Neba.Api.Contracts.Security;

/// <summary>Defines the Security API contract for authentication and account management.</summary>
public interface ISecurityApi
{
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
    Task<IApiResponse<GetCurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets any user's password directly (Admin only). No current password or email token required.</summary>
    [Post("/security/password/reset")]
    Task<IApiResponse> ResetPasswordAsync([Body] ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets a new password using a token (invite/reset), and confirms the user's email. Anonymous.</summary>
    [Post("/security/password/set-from-token")]
    Task<IApiResponse> SetPasswordFromTokenAsync([Body] SetPasswordFromTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a new user account. No password set — an invitation email is sent.</summary>
    [Post("/security/users")]
    Task<IApiResponse<CreateUserResponse>> CreateUserAsync([Body] CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists user accounts, including email confirmation status and assigned roles.</summary>
    [Get("/security/users")]
    Task<IApiResponse<PaginationResponse<UserSummaryResponse>>> ListUsersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}