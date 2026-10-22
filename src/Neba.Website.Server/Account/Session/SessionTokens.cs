namespace Neba.Website.Server.Account.Session;

/// <summary>
/// The token pair and identity stored in the auth cookie for one signed-in user.
/// </summary>
internal sealed record SessionTokens(string UserId, string Email, string AccessToken, string RefreshToken);