namespace Neba.Api.Legacy.Tournaments.Stats;

// DateOfBirth is DateTime, not DateOnly - Bowlers.DateOfBirth is a legacy date column, but Dapper's
// constructor-based row materialization requires the declared parameter type to match the reader's
// column type exactly, and Microsoft.Data.SqlClient (the real neba-fwk provider) surfaces a SQL
// `date` column as DateTime, not DateOnly (unlike Npgsql). See LegacyCupResultRow for the same
// convention. Callers convert via DateOnly.FromDateTime at the point of use.
internal sealed record LegacyBowlerRow(int BowlerId, int? Gender, DateTime? DateOfBirth);