using StronglyTypedIds;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Unique identifier for a Series.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeriesId;
