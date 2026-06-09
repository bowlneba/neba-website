using StronglyTypedIds;

namespace Neba.Api.Security.Domain;

/// <summary>
/// Unique identifier for a user.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct RoleId;