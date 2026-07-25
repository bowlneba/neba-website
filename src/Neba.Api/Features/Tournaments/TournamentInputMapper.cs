using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Maps the string/primitive fields of <see cref="TournamentInput"/> to their domain-typed
/// equivalents. Shared by the create and edit endpoints, which build different command types
/// from the same input shape.
/// </summary>
internal static class TournamentInputMapper
{
    public static CertificationNumber? ToBowlingCenterId(string? certificationNumber) =>
        string.IsNullOrWhiteSpace(certificationNumber)
            ? null
            : new CertificationNumber { Value = certificationNumber };

    public static StoredFile? ToLogo(TournamentLogoInput? logo) =>
        logo is null
            ? null
            : new StoredFile
            {
                Container = logo.Container,
                Path = logo.Path,
                ContentType = logo.ContentType,
                SizeInBytes = logo.SizeInBytes
            };

    public static OilPatternId? ToOilPatternId(string? oilPatternId) =>
        string.IsNullOrWhiteSpace(oilPatternId)
            ? null
            : new OilPatternId(oilPatternId);

    public static PatternLengthCategory? ToPatternLengthCategory(string? patternLengthCategory) =>
        string.IsNullOrWhiteSpace(patternLengthCategory)
            ? null
            : PatternLengthCategory.FromName(patternLengthCategory);

    public static PatternRatioCategory? ToPatternRatioCategory(string? patternRatioCategory) =>
        string.IsNullOrWhiteSpace(patternRatioCategory)
            ? null
            : PatternRatioCategory.FromName(patternRatioCategory);
}