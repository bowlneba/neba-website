using System.Diagnostics;

using ErrorOr;

using Neba.Api.Contracts.Memberships;

namespace Neba.Api.Features.Memberships.Domain;

/// <summary>
/// The membership rules: end date and eligibility. Pure and stateless; callers pass dates derived from <see cref="TimeProvider"/>.
/// </summary>
internal static class MembershipRules
{
    private const int YearlyRolloverMonth = 11;

    extension(MembershipType type)
    {
        /// <summary>
        /// The last day a membership of this type bought on <paramref name="start"/> is valid. Inclusive.
        /// </summary>
        public DateOnly CalculateEndDate(DateOnly start)
            => type.Kind switch
            {
                MembershipKind.Temporary => start.AddDays(type.LengthInDays!.Value - 1),
                MembershipKind.Yearly => new DateOnly(start.Month >= YearlyRolloverMonth ? start.Year + 1 : start.Year, 12, 31),
                _ => throw new UnreachableException($"Unknown membership kind '{type.Kind}'.")
            };
    }

    /// <summary>
    /// Checks whether a bowler with <paramref name="history"/> may buy <paramref name="type"/> starting on <paramref name="start"/>.
    /// Returns the first rule broken.
    /// </summary>
    public static ErrorOr<Success> CheckEligibility(
        MembershipType type,
        DateOnly start,
        DateOnly end,
        IReadOnlyCollection<MembershipSnapshot> history)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(history);

        if (!type.Active)
        {
            return BowlerMembershipErrors.TypeInactive(type);
        }

        if (type.RequiresPriorMembership && !history.Any(held => held.Type.QualifiesAsPriorMembership))
        {
            return BowlerMembershipErrors.RequiresPriorMembership(type);
        }

        var repeatError = CheckRepeatPolicy(type, start, history);
        if (repeatError is not null)
        {
            return repeatError.Value;
        }

        var current = history.Where(held => held.EndDate >= start).ToArray();

        if (type.Kind == MembershipKind.Temporary && current.Any(held => held.Type.Kind == MembershipKind.Temporary))
        {
            var activeEndDate = current.Where(held => held.Type.Kind == MembershipKind.Temporary).Max(held => held.EndDate);
            return BowlerMembershipErrors.TemporaryMembershipActive(activeEndDate);
        }

        if (current.Any(held => end <= held.EndDate))
        {
            return BowlerMembershipErrors.ExpiresBeforeCurrent(end, current.Max(held => held.EndDate));
        }

        return Result.Success;
    }

    private static Error? CheckRepeatPolicy(
        MembershipType type,
        DateOnly start,
        IReadOnlyCollection<MembershipSnapshot> history)
    {
        var policy = type.RepeatPolicy;

        switch (policy.Kind)
        {
            case RepeatPolicyKind.Unlimited:
                return null;

            case RepeatPolicyKind.OncePerLifetime:
                return history.Any(held => held.Type == type)
                    ? BowlerMembershipErrors.AlreadyHeld(type)
                    : null;

            case RepeatPolicyKind.MinimumDaysSinceLast:
                if (history.Count == 0)
                {
                    return null;
                }

                var lastStartDate = history.Max(held => held.StartDate);
                var daysRequired = policy.Days!.Value;

                return start.DayNumber - lastStartDate.DayNumber < daysRequired
                    ? BowlerMembershipErrors.TooSoonSinceLast(type, daysRequired, lastStartDate)
                    : null;

            default:
                throw new UnreachableException($"Unknown repeat policy kind '{policy.Kind}'.");
        }
    }
}