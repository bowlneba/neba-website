using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// What the team eligibility rules need to know about one team member.
/// </summary>
internal sealed record TeamMemberFacts
{
    /// <summary>Null for a bowler who is not in the system.</summary>
    public BowlerId? BowlerId { get; init; }

    /// <summary>Null when the date of birth is not on file, and for a bowler who is not in the system.</summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>The NEBA titles the bowler has won.</summary>
    public IReadOnlyCollection<NebaTitle> Titles { get; init; } = [];

    /// <summary>True when the bowler is a Hall of Fame member for Superior Performance.</summary>
    public bool HallOfFameSuperiorPerformance { get; init; }
}
