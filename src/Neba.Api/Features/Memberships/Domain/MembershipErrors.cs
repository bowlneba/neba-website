using ErrorOr;

using Neba.Api.Contracts.Memberships;

namespace Neba.Api.Features.Memberships.Domain;

internal static class MembershipErrors
{
    public static Error TypeInactive(MembershipType type) => Error.Validation(
        code: "BowlerMembership.TypeInactive",
        description: $"Membership type '{type.Name}' can no longer be bought.",
        metadata: new Dictionary<string, object> { { "MembershipType", type.Value } });

    public static Error RequiresPriorMembership(MembershipType type) => Error.Conflict(
        code: "BowlerMembership.RequiresPriorMembership",
        description: $"Membership type '{type.Name}' needs a prior membership. Buy a new membership first.",
        metadata: new Dictionary<string, object> { { "MembershipType", type.Value } });

    public static Error AlreadyHeld(MembershipType type) => Error.Conflict(
        code: "BowlerMembership.AlreadyHeld",
        description: $"Membership type '{type.Name}' can be held only once and the bowler already held it.",
        metadata: new Dictionary<string, object> { { "MembershipType", type.Value } });

    public static Error TooSoonSinceLast(MembershipType type, int daysRequired, DateOnly lastStartDate) => Error.Conflict(
        code: "BowlerMembership.TooSoonSinceLast",
        description: $"Membership type '{type.Name}' needs at least {daysRequired} days since the last membership started on {lastStartDate:yyyy-MM-dd}.",
        metadata: new Dictionary<string, object>
        {
            { "MembershipType", type.Value },
            { "DaysRequired", daysRequired },
            { "LastStartDate", lastStartDate }
        });

    public static Error TemporaryMembershipActive(DateOnly activeEndDate) => Error.Conflict(
        code: "BowlerMembership.TemporaryMembershipActive",
        description: $"The bowler already has a temporary membership valid through {activeEndDate:yyyy-MM-dd}.",
        metadata: new Dictionary<string, object> { { "ActiveEndDate", activeEndDate } });

    public static Error ExpiresBeforeCurrent(DateOnly endDate, DateOnly currentEndDate) => Error.Conflict(
        code: "BowlerMembership.ExpiresBeforeCurrent",
        description: $"The new membership would end on {endDate:yyyy-MM-dd}, on or before the current membership ends on {currentEndDate:yyyy-MM-dd}.",
        metadata: new Dictionary<string, object>
        {
            { "EndDate", endDate },
            { "CurrentEndDate", currentEndDate }
        });
}
