using Neba.Api.Contracts.Memberships;

namespace Neba.Api.Features.Memberships.Domain;

/// <summary>
/// The facts about a held membership that the eligibility rules read.
/// </summary>
/// <param name="Type">The membership type held.</param>
/// <param name="StartDate">The first day the membership was valid.</param>
/// <param name="EndDate">The last day the membership is valid. Inclusive.</param>
internal readonly record struct MembershipSnapshot(MembershipType Type, DateOnly StartDate, DateOnly EndDate);
