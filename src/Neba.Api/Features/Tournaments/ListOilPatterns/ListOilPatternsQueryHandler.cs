using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed class ListOilPatternsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>>
{
    private readonly IQueryable<OilPattern> _oilPatterns = appDbContext.OilPatterns.AsNoTracking();

    public async Task<IReadOnlyCollection<OilPatternSummaryDto>> HandleAsync(ListOilPatternsQuery query, CancellationToken cancellationToken)
    {
        var rows = await _oilPatterns
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Length,
                p.Volume,
                p.LeftRatio,
                p.RightRatio,
                p.KegelId
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new OilPatternSummaryDto
        {
            Id = row.Id,
            Name = row.Name,
            Length = row.Length,
            Volume = row.Volume,
            LeftRatio = row.LeftRatio,
            RightRatio = row.RightRatio,
            KegelId = row.KegelId,
            LengthCategory = PatternLengthCategory.FromLength(row.Length).Name,
            RatioCategory = PatternRatioCategory.FromRatio(Math.Max(row.LeftRatio, row.RightRatio)).Name
        })];
    }
}