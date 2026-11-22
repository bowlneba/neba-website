using FluentValidation;

namespace Neba.Api.Features.Series;

internal static class SeriesRequestRules
{
    extension<T>(IRuleBuilder<T, string> rule)
    {
        /// <summary>Requires a non-empty 26-character ULID string (structure only).</summary>
        public IRuleBuilderOptions<T, string> IsUlid(string label, string codePrefix)
            => rule.NotEmpty().WithErrorCode($"{codePrefix}Required").WithMessage($"{label} is required.")
                .Length(26).WithErrorCode($"{codePrefix}InvalidLength").WithMessage($"{label} must be a 26-character ULID.");
    }
}