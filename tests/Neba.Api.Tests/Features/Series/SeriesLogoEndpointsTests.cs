using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesLogoEndpointsTests(AppDbContextFixture fixture, SecurityDbContextFixture securityFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private SeriesApiHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await securityFixture.ResetAsync();
        _host = await SeriesApiHost.StartAsync(fixture, securityFixture);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        await fixture.ResetAsync();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        using var request = SeriesApiHost.Request(method, url, token, body);
        return await _host.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> UploadAsync(string? token)
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3, 4]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "File", "logo.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/series/logo", UriKind.Relative)) { Content = content };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _host.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<SeriesAggregate> SeedSeriesAsync(Neba.Api.Features.Storage.Domain.StoredFile? logo = null, StageStatus? status = null)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        return await SeriesSeed.AddSeriesAsync(db, season, new DateOnly(2025, 2, 1), ct, name: "VH", logo: logo, status: status);
    }

    private async Task StageAsync(string path)
    {
        await using var db = fixture.CreateDbContext();
        db.PendingUploads.Add(PendingUploadFactory.Create(container: "series", path: path));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string TokenWith(params Permissions[] permissions)
        => TestAccessTokenFactory.Create(permissions: permissions);

    // ---- POST /series/logo ----

    [Theory(DisplayName = "POST /series/logo stages a file for CreateSeries, EditSeries and EditSeriesLogo")]
    [InlineData("Series.CreateSeries")]
    [InlineData("Series.EditSeries")]
    [InlineData("Series.EditSeriesLogo")]
    public async Task Upload_ShouldReturn200_ForEachUploadPermission(string permission)
    {
        // Arrange
        var token = TokenWith(Permissions.FromValue(permission));

        // Act
        using var response = await UploadAsync(token);
        var body = await response.Content.ReadFromJsonAsync<UploadedFileResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Path.ShouldBe("series/logo/staged.png");
    }

    [Fact(DisplayName = "POST /series/logo returns 401 without a token and 403 without a Series logo permission")]
    public async Task Upload_ShouldReturn401And403_WhenNotAuthorized()
    {
        // Arrange & Act
        using var anonymous = await UploadAsync(null);
        using var deleteOnly = await UploadAsync(TokenWith(Permissions.DeleteSeries));
        using var member = await UploadAsync(SeriesApiHost.MemberToken());

        // Assert
        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        deleteOnly.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        member.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ---- PUT /series/{id}/logo ----

    [Fact(DisplayName = "PUT /series/{id}/logo sets, replaces and removes the logo")]
    public async Task EditLogo_ShouldSetReplaceAndRemove()
    {
        // Arrange
        var series = await SeedSeriesAsync();
        var token = TokenWith(Permissions.EditSeriesLogo);
        var url = $"/series/{series.Id.Value}/logo";

        // Act
        await StageAsync("first.png");
        using var set = await SendAsync(HttpMethod.Put, url, token, new EditSeriesLogoRequest
        {
            Id = series.Id.Value.ToString(),
            Input = new EditSeriesLogoInput { Logo = SeriesLogoInputFactory.Create(container: "series", path: "first.png") },
        });
        var afterSet = await GetAsync(series);

        await StageAsync("second.png");
        using var replace = await SendAsync(HttpMethod.Put, url, token, new EditSeriesLogoRequest
        {
            Id = series.Id.Value.ToString(),
            Input = new EditSeriesLogoInput { Logo = SeriesLogoInputFactory.Create(container: "series", path: "second.png") },
        });
        var afterReplace = await GetAsync(series);

        using var remove = await SendAsync(HttpMethod.Put, url, token, new EditSeriesLogoRequest
        {
            Id = series.Id.Value.ToString(),
            Input = new EditSeriesLogoInput { Logo = null },
        });
        var afterRemove = await GetAsync(series);

        // Assert
        set.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        afterSet.LogoPath.ShouldBe("first.png");
        afterSet.LogoUrl.ShouldNotBeNull();
        replace.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        afterReplace.LogoPath.ShouldBe("second.png");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        afterRemove.LogoUrl.ShouldBeNull();
        afterRemove.LogoPath.ShouldBeNull();
    }

    [Fact(DisplayName = "PUT /series/{id}/logo works on a locked Series")]
    public async Task EditLogo_ShouldWork_WhenLocked()
    {
        // Arrange
        var series = await SeedSeriesAsync(status: StageStatus.Computed);
        await StageAsync("locked.png");

        // Act
        using var response = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}/logo", TokenWith(Permissions.EditSeriesLogo), new EditSeriesLogoRequest
        {
            Id = series.Id.Value.ToString(),
            Input = new EditSeriesLogoInput { Logo = SeriesLogoInputFactory.Create(container: "series", path: "locked.png") },
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PUT /series/{id}/logo returns 403 without EditSeriesLogo, 404 for an unknown Series and 422 for an unstaged file")]
    public async Task EditLogo_ShouldReturn403_404_And422()
    {
        // Arrange
        var series = await SeedSeriesAsync();
        var request = new EditSeriesLogoRequest
        {
            Id = series.Id.Value.ToString(),
            Input = new EditSeriesLogoInput { Logo = SeriesLogoInputFactory.Create(container: "series", path: "never-staged.png") },
        };

        // Act
        using var forbidden = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}/logo", TokenWith(Permissions.EditSeries), request);
        var unknownId = SeriesId.New().Value.ToString();
        using var notFound = await SendAsync(HttpMethod.Put, $"/series/{unknownId}/logo", TokenWith(Permissions.EditSeriesLogo), request with { Id = unknownId });
        using var unstaged = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}/logo", TokenWith(Permissions.EditSeriesLogo), request);

        // Assert
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unstaged.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // ---- stored denial on the full edit ----

    [Fact(DisplayName = "PUT /series/{id} with a changed logo returns 403 for a caller with a stored EditSeriesLogo denial, and allows an unchanged logo")]
    public async Task Edit_ShouldReturn403_WhenCallerIsDeniedLogoChange()
    {
        // Arrange
        var stored = StoredFileFactory.Create(container: "series", path: "kept.png", contentType: "image/png", sizeInBytes: 10);
        var series = await SeedSeriesAsync(stored);
        await StageAsync("changed.png");

        using var scope = securityFixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = ApplicationUserFactory.Create(emailConfirmed: true);
        (await userManager.CreateAsync(user)).Succeeded.ShouldBeTrue();
        await userManager.AddClaimAsync(user, new Claim(Permissions.DeniedClaimType, Permissions.EditSeriesLogo.Value));

        var token = TestAccessTokenFactory.Create(permissions: [Permissions.EditSeries], userId: user.Id);
        var detail = await GetAsync(series);
        var stages = detail.Stages.Select(s => SeriesStageInputFactory.Create(
            s.Id, s.Kind, s.TournamentId, s.AdvancingCount, s.SquadDate, s.SquadStartTime)).ToList();

        EditSeriesRequest Request(SeriesLogoInput? logo) => new()
        {
            Id = series.Id.Value.ToString(),
            Series = new EditSeriesInput { Name = "Renamed", Logo = logo, Stages = stages },
        };

        // Act
        using var changed = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token,
            Request(SeriesLogoInputFactory.Create(container: "series", path: "changed.png")));
        using var unchanged = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token,
            Request(SeriesLogoInputFactory.Create(container: "series", path: "kept.png")));

        // Assert
        changed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unchanged.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<SeriesDetailResponse> GetAsync(SeriesAggregate series)
    {
        var detail = await _host.Client.GetFromJsonAsync<SeriesDetailResponse>(
            new Uri($"/series/{series.Id.Value}", UriKind.Relative), TestContext.Current.CancellationToken);

        return detail!;
    }
}
