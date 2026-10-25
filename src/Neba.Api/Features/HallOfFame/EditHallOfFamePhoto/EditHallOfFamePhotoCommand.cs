using ErrorOr;

using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;

internal sealed record EditHallOfFamePhotoCommand
    : ICommand<Updated>
{
    public required HallOfFameId HallOfFameId { get; init; }

    public StoredFile? Photo { get; init; }

    public int? FocalX { get; init; }

    public int? FocalY { get; init; }

    public int? Zoom { get; init; }
}