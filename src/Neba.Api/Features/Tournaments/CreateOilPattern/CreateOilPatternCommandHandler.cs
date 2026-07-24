using EntityFramework.Exceptions.Common;

using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateOilPatternCommand, CreatedOilPattern>
{
    public async Task<ErrorOr<CreatedOilPattern>> HandleAsync(CreateOilPatternCommand command, CancellationToken cancellationToken)
    {
        var oilPatternResult = OilPattern.Create(
            name: command.Name,
            length: command.Length,
            volume: command.Volume,
            leftRatio: command.LeftRatio,
            rightRatio: command.RightRatio,
            kegelId: command.KegelId);

        if (oilPatternResult.IsError)
        {
            return oilPatternResult.Errors;
        }

        var oilPattern = oilPatternResult.Value;

        await appDbContext.OilPatterns.AddAsync(oilPattern, cancellationToken);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException ex) when (ex.ConstraintProperties.Contains(nameof(OilPattern.KegelId)))
        {
            return OilPatternErrors.KegelIdAlreadyExists(command.KegelId!.Value);
        }

        await cache.RemoveByTagAsync("neba:oil-patterns", token: cancellationToken);

        return new CreatedOilPattern
        {
            Id = oilPattern.Id,
            Name = oilPattern.Name,
            Length = oilPattern.Length,
            LengthCategory = oilPattern.LengthCategory,
            RatioCategory = oilPattern.RatioCategory
        };
    }
}