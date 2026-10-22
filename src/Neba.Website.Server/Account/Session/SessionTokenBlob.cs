namespace Neba.Website.Server.Account.Session;

/// <summary>An encrypted <see cref="SessionTokens"/> payload.</summary>
internal sealed record SessionTokenBlob(string Blob);