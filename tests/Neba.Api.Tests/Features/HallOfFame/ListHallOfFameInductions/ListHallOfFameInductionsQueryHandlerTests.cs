using Neba.Api.Database;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFameInductions;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<AppDbContextFixture>]
public sealed class ListHallOfFameInductionsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "HandleAsync returns empty collection when no inductions exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoInductionsExist()
    {
        // Arrange
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListHallOfFameInductionsQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            new ListHallOfFameInductionsQuery(),
            TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns induction with correct fields when data exists")]
    public async Task HandleAsync_ShouldReturnInduction_WithCorrectFields_WhenDataExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var induction = HallOfFameInductionFactory.Create(
            year: 2020,
            bowlerId: bowler.Id,
            categories: [HallOfFameCategory.SuperiorPerformance]);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListHallOfFameInductionsQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(new ListHallOfFameInductionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Year.ShouldBe(2020);
        dto.BowlerName.ShouldBe(bowler.Name);
        dto.Categories.ShouldContain(HallOfFameCategory.SuperiorPerformance);
        dto.PhotoUri.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync orders results by last name then first name")]
    public async Task HandleAsync_ShouldOrderByLastNameThenFirstName_WhenMultipleInductionsExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var zach = BowlerFactory.Create(name: NameFactory.Create("Zach", "Adams"));
        var amanda = BowlerFactory.Create(name: NameFactory.Create("Amanda", "Baker"));
        var bob = BowlerFactory.Create(name: NameFactory.Create("Bob", "Baker"));
        await _dbContext.Bowlers.AddRangeAsync([zach, amanda, bob], ct);

        await _dbContext.HallOfFameInductions.AddRangeAsync(
            [
                HallOfFameInductionFactory.Create(bowlerId: bob.Id),
                HallOfFameInductionFactory.Create(bowlerId: zach.Id),
                HallOfFameInductionFactory.Create(bowlerId: amanda.Id)
            ],
            ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListHallOfFameInductionsQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(new ListHallOfFameInductionsQuery(), ct);

        // Assert
        // Sorted by last name (Adams, Baker, Baker), then first name within the tied last name (Amanda before Bob) —
        // insertion order was intentionally different so this proves the query orders rather than preserving insert order.
        result.Select(dto => dto.BowlerName).ShouldBe([zach.Name, amanda.Name, bob.Name]);
    }

    [Fact(DisplayName = "HandleAsync sets PhotoUri when induction has a photo")]
    public async Task HandleAsync_ShouldSetPhotoUri_WhenInductionHasPhoto()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var photo = StoredFileFactory.Create(container: "photos", path: "hall-of-fame/jane-doe.jpg");
        var induction = HallOfFameInductionFactory.Create(bowlerId: bowler.Id, photo: photo);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/photos/hall-of-fame/jane-doe.jpg");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("photos", "hall-of-fame/jane-doe.jpg"))
            .Returns(expectedUri);
        var handler = new ListHallOfFameInductionsQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(new ListHallOfFameInductionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().PhotoUri.ShouldBe(expectedUri);
    }
}