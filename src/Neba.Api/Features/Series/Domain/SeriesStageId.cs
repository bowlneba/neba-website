using StronglyTypedIds;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Unique identifier for a Stage within a Series. Stable across edits.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeriesStageId;
