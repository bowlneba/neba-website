using Neba.Application.HallOfFame;
using Neba.Application.HallOfFame.ListHallOfFameInductions;
using Neba.Application.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Application.Tests.HallOfFame.ListHallOfFameInductions;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFameInductionsQueryHandlerTests
{
    private readonly Mock<IHallOfFameQueries> _hallOfFameQueriesMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;

    private readonly ListHallOfFameInductionsQueryHandler _handler;

    public ListHallOfFameInductionsQueryHandlerTests()
    {
        _hallOfFameQueriesMock = new Mock<IHallOfFameQueries>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);

        _handler = new ListHallOfFameInductionsQueryHandler(
            _hallOfFameQueriesMock.Object,
            _fileStorageServiceMock.Object);
    }

    [Fact(DisplayName = "Should return empty collection when no inductions exist")]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoInductionsExist()
    {
        // Arrange
        var query = new ListHallOfFameInductionsQuery();

        _hallOfFameQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should return inductions with null PhotoUri when no inductions have photos")]
    public async Task HandleAsync_ShouldReturnInductionsWithNullPhotoUri_WhenNoInductionsHavePhotos()
    {
        // Arrange
        var query = new ListHallOfFameInductionsQuery();
        var inductions = new[]
        {
            HallOfFameInductionDtoFactory.Create(photoContainer: null, photoPath: null),
            HallOfFameInductionDtoFactory.Create(photoContainer: null, photoPath: null)
        };

        _hallOfFameQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(inductions);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(i => i.PhotoUri == null);
    }

    [Fact(DisplayName = "Should set PhotoUri on all inductions when all have photos")]
    public async Task HandleAsync_ShouldSetPhotoUri_WhenAllInductionsHavePhotos()
    {
        // Arrange
        var query = new ListHallOfFameInductionsQuery();
        var photoUri1 = new Uri("https://storage.example.com/hof/bowler1.jpg");
        var photoUri2 = new Uri("https://storage.example.com/hof/bowler2.jpg");
        var induction1 = HallOfFameInductionDtoFactory.Create(photoContainer: "hof-photos", photoPath: "bowler1.jpg");
        var induction2 = HallOfFameInductionDtoFactory.Create(photoContainer: "hof-photos", photoPath: "bowler2.jpg");

        _hallOfFameQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([induction1, induction2]);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri("hof-photos", "bowler1.jpg"))
            .Returns(photoUri1);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri("hof-photos", "bowler2.jpg"))
            .Returns(photoUri2);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(i => i.PhotoUri != null);
    }

    [Fact(DisplayName = "Should set PhotoUri only on inductions with photos in a mixed collection")]
    public async Task HandleAsync_ShouldSetPhotoUriOnlyForInductionsWithPhotos_WhenCollectionIsMixed()
    {
        // Arrange
        var query = new ListHallOfFameInductionsQuery();
        var photoUri = new Uri("https://storage.example.com/hof/bowler.jpg");
        var inductionWithPhoto = HallOfFameInductionDtoFactory.Create(photoContainer: "hof-photos", photoPath: "bowler.jpg");
        var inductionWithoutPhoto = HallOfFameInductionDtoFactory.Create();

        _hallOfFameQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([inductionWithPhoto, inductionWithoutPhoto]);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri("hof-photos", "bowler.jpg"))
            .Returns(photoUri);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.Single(i => i.PhotoContainer is not null).PhotoUri.ShouldBe(photoUri);
        result.Single(i => i.PhotoContainer is null).PhotoUri.ShouldBeNull();
    }
}