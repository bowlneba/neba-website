using Neba.Api.Features.HallOfFame.Domain;

namespace Neba.TestFactory.HallOfFame;

public static class PhotoFocalPointFactory
{
    public const int DefaultX = 42;
    public const int DefaultY = 28;
    public const int DefaultZoom = 150;

    public static PhotoFocalPoint Create(
        int? x = null,
        int? y = null,
        int? zoom = null)
        => new()
        {
            X = x ?? DefaultX,
            Y = y ?? DefaultY,
            Zoom = zoom ?? DefaultZoom
        };
}
