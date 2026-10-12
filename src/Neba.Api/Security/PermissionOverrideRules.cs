using System.Linq.Expressions;

using FluentValidation;

using Neba.Api.Contracts.Security;

namespace Neba.Api.Security;

internal static class PermissionOverrideRules
{
    public static void ApplyPermissionOverrideRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, IEnumerable<string>>> added,
        Expression<Func<T, IEnumerable<string>>> removed,
        string errorCodePrefix)
    {
        var addedValues = added.Compile();
        var removedValues = removed.Compile();

        validator.RuleForEach(added)
            .Must(IsKnownPermission)
            .WithErrorCode($"{errorCodePrefix}.AddedPermissionUnknown")
            .WithMessage("One or more added permissions are not recognized.");

        validator.RuleForEach(removed)
            .Must(IsKnownPermission)
            .WithErrorCode($"{errorCodePrefix}.RemovedPermissionUnknown")
            .WithMessage("One or more removed permissions are not recognized.");

        validator.RuleFor(added)
            .Must(HasNoDuplicates)
            .WithErrorCode($"{errorCodePrefix}.AddedPermissionDuplicate")
            .WithMessage("Added permissions must be unique.");

        validator.RuleFor(removed)
            .Must(HasNoDuplicates)
            .WithErrorCode($"{errorCodePrefix}.RemovedPermissionDuplicate")
            .WithMessage("Removed permissions must be unique.");

        validator.RuleFor(request => request)
            .Must(request => !addedValues(request).Intersect(removedValues(request), StringComparer.Ordinal).Any())
            .WithErrorCode($"{errorCodePrefix}.PermissionAddedAndRemoved")
            .WithMessage("A permission can't be both added and removed.");
    }

    private static bool IsKnownPermission(string value)
        => Permissions.TryFromValue(value, out _);

    private static bool HasNoDuplicates(IEnumerable<string> values)
    {
        var list = values.ToList();
        return list.Distinct(StringComparer.Ordinal).Count() == list.Count;
    }
}
