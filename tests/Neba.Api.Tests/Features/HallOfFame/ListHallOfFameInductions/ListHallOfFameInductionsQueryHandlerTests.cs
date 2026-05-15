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
[Collection<PostgreSqlFixture>]
public sealed class ListHallOfFameInductionsQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
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
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListHallOfFameInductionsQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new ListHallOfFameInductionsQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns induction with correct fields when data exists")]
    public async Task HandleAsync_ShouldReturnInduction_WithCorrectFields_WhenDataExists()
    {
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

        var result = await handler.HandleAsync(new ListHallOfFameInductionsQuery(), ct);

        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Year.ShouldBe(2020);
        dto.BowlerName.ShouldBe(bowler.Name);
        dto.Categories.ShouldContain(HallOfFameCategory.SuperiorPerformance);
        dto.PhotoUri.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync sets PhotoUri when induction has a photo")]
    public async Task HandleAsync_ShouldSetPhotoUri_WhenInductionHasPhoto()
    {
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

        var result = await handler.HandleAsync(new ListHallOfFameInductionsQuery(), ct);

        result.ShouldHaveSingleItem();
        result.Single().PhotoUri.ShouldBe(expectedUri);
    }
}
