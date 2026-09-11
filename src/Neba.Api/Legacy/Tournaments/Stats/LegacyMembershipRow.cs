namespace Neba.Api.Legacy.Tournaments.Stats;

// EndDate is DateTime, not DateOnly - see LegacyBowlerRow's DateOfBirth for the same
// Microsoft.Data.SqlClient/Dapper convention. Callers convert via DateOnly.FromDateTime at the
// point of use.
internal sealed record LegacyMembershipRow(int BowlerId, int MembershipId, DateTime EndDate);