using Neba.Api.Contracts.Memberships;

namespace Neba.Api.Legacy.Tournaments.Stats;

// Read from the website's bowler_memberships table, keyed by the bowler's legacy id because the rest of the
// stats calculation still keys on legacy ids.
internal sealed record MembershipRow(int BowlerId, MembershipType Type, DateOnly EndDate);
