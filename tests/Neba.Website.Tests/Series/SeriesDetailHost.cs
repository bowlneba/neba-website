using System.Net;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Series;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Series;

/// <summary>
/// The bUnit host for the Series page: strict Series API, an identity time zone service and the viewer's permissions.
/// </summary>
internal sealed class SeriesDetailHost : IDisposable
{
    public const string SeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";

    private readonly List<IDisposable> _disposables = [];

    public SeriesDetailHost()
    {
        Ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
        Auth = Ctx.AddAuthorization();
        Auth.SetNotAuthorized();

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        TimeZone.Setup(x => x.ToLocalAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync((DateTimeOffset utc) => utc);

        Ctx.Services.AddSingleton(Api.Object);
        Ctx.Services.AddSingleton(TimeZone.Object);
        Ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        Ctx.Services.AddSingleton(Toasts);
        Ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public BunitContext Ctx { get; } = new();

    public BunitAuthorizationContext Auth { get; }

    public Mock<ISeriesApi> Api { get; } = new(MockBehavior.Strict);

    public Mock<IClientTimeZoneService> TimeZone { get; } = new(MockBehavior.Strict);

    public ToastService Toasts { get; } = new();

    public void Dispose()
    {
        Ctx.Dispose();
        Toasts.Dispose();
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    public IRenderedComponent<SeriesDetail> Render() => Ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

    /// <summary>Holds Series.EditSeries only.</summary>
    public void SignInAsManager()
    {
        Auth.SetAuthorized("test-user");
        Auth.SetPolicies(Permissions.EditSeries.PolicyName);
    }

    /// <summary>Holds Series.EditSeries and Series.ManageAdvancement.</summary>
    public void SignInAsAdmin()
    {
        Auth.SetAuthorized("test-user");
        Auth.SetPolicies(Permissions.EditSeries.PolicyName, Permissions.ManageAdvancement.PolicyName);
    }

    /// <summary>Signed in, with a Series permission that is not Series.EditSeries.</summary>
    public void SignInWithoutEditSeries()
    {
        Auth.SetAuthorized("test-user");
        Auth.SetPolicies(Permissions.EditSeriesLogo.PolicyName);
    }

    public void SetupDetail(SeriesDetailResponse detail)
    {
        var response = Track(Stub(HttpStatusCode.OK, detail));
        Api.Setup(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    /// <summary>Sets up GetSeries with one Stage per advancement Stage (same ids, orders and kinds).</summary>
    public void SetupDetailFor(SeriesAdvancementResponse advancement, Uri? logo = null)
    {
        var stages = advancement.Stages
            .OrderBy(s => s.Order)
            .Select(s => SeriesStageResponseFactory.Create(
                id: s.StageId,
                order: s.Order,
                kind: s.Kind,
                advancingCount: s.Kind == "Placement" ? null : 8,
                tournamentId: s.TournamentId,
                tournamentName: "Tournament " + s.Order))
            .ToArray();

        SetupDetail(SeriesDetailResponseFactory.Create(id: SeriesId, finale: SeriesFinaleResponseFactory.Create(), logoUrl: logo, stages: stages));
    }

    public void SetupAdvancement(SeriesAdvancementResponse advancement)
    {
        var response = Track(Stub(HttpStatusCode.OK, advancement));
        Api.Setup(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    public void SetupAdvancementStatus(HttpStatusCode status)
    {
        var response = Track(Stub<SeriesAdvancementResponse>(status, null));
        Api.Setup(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    /// <summary>The first advancement call fails with <paramref name="firstStatus"/>; the second returns <paramref name="then"/>.</summary>
    public void SetupAdvancementFailureThenSuccess(HttpStatusCode firstStatus, SeriesAdvancementResponse then)
    {
        var failure = Track(Stub<SeriesAdvancementResponse>(firstStatus, null));
        var success = Track(Stub(HttpStatusCode.OK, then));
        Api.SetupSequence(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure)
            .ReturnsAsync(success);
    }

    public void SetupAdvancementPending(TaskCompletionSource<IApiResponse<SeriesAdvancementResponse>> completion)
        => Api.Setup(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>())).Returns(completion.Task);

    /// <summary>A completed OK response for a <see cref="TaskCompletionSource{TResult}"/>.</summary>
    public IApiResponse<SeriesAdvancementResponse> OkResponse(SeriesAdvancementResponse advancement)
        => Track(Stub(HttpStatusCode.OK, advancement));

    /// <summary>The advancement calls return <paramref name="advancements"/> in order: the load, then each reload.</summary>
    public void SetupAdvancementSequence(params SeriesAdvancementResponse[] advancements)
        => SetupAdvancementSequence([.. advancements.Select(OkResponse)]);

    /// <summary>The advancement calls return <paramref name="responses"/> in order; use <see cref="FailedAdvancementResponse"/> for a failed reload.</summary>
    public void SetupAdvancementSequence(params IApiResponse<SeriesAdvancementResponse>[] responses)
    {
        var sequence = Api.SetupSequence(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>()));
        foreach (var response in responses)
        {
            sequence.ReturnsAsync(response);
        }
    }

    /// <summary>A failed advancement response with no body.</summary>
    public IApiResponse<SeriesAdvancementResponse> FailedAdvancementResponse(HttpStatusCode status)
        => Track(Stub<SeriesAdvancementResponse>(status, null));

    /// <summary>A 204 for a write.</summary>
    public IApiResponse NoContent()
        => Track(new StubApiResponse<object> { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.NoContent });

    /// <summary>A failed write with no body (ApiExecutor gives a 404 NotFound, anything else a Failure).</summary>
    public IApiResponse FailedWrite(HttpStatusCode status)
        => Track(new StubApiResponse<object> { IsSuccessStatusCode = false, StatusCode = status });

    /// <summary>A failed write whose problem details carry <paramref name="message"/> (409 gives Conflict, 422 gives Validation).</summary>
    public async Task<IApiResponse> RejectedWriteAsync(HttpStatusCode status, string message)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/series");
        using var body = new HttpResponseMessage(status)
        {
            Content = new StringContent("{\"status\":" + (int)status + ",\"title\":\"Error\",\"errors\":[{\"name\":\"generalErrors\",\"reason\":\"" + message + "\"}]}"),
        };
        var exception = await ApiException.Create(request, HttpMethod.Post, body, new RefitSettings());
        return Track(new StubApiResponse<object> { IsSuccessStatusCode = false, StatusCode = status, Error = exception });
    }

    /// <summary>Sets up the recompute write for <paramref name="stageId"/>; returns the requests it receives.</summary>
    public List<RecomputeSeriesAdvancementRequest> SetupRecompute(string stageId, Func<Task<IApiResponse>> respond)
    {
        var requests = new List<RecomputeSeriesAdvancementRequest>();
        Api.Setup(x => x.RecomputeSeriesAdvancementAsync(SeriesId, stageId, It.IsAny<RecomputeSeriesAdvancementRequest>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string _, RecomputeSeriesAdvancementRequest request, CancellationToken _) =>
            {
                requests.Add(request);
                return respond();
            });
        return requests;
    }

    public List<RecomputeSeriesAdvancementRequest> SetupRecompute(string stageId, IApiResponse response)
        => SetupRecompute(stageId, () => Task.FromResult(response));

#pragma warning disable VSTHRD003 // The test owns the completion source and completes it itself.
    /// <summary>The recompute write stays pending until the test completes <paramref name="completion"/>.</summary>
    public List<RecomputeSeriesAdvancementRequest> SetupRecompute(string stageId, TaskCompletionSource<IApiResponse> completion)
        => SetupRecompute(stageId, () => completion.Task);

    /// <summary>The Pick squad write stays pending until the test completes <paramref name="completion"/>.</summary>
    public List<PickStageSquadRequest> SetupPickSquad(string stageId, TaskCompletionSource<IApiResponse> completion)
        => SetupPickSquad(stageId, () => completion.Task);
#pragma warning restore VSTHRD003

    /// <summary>Sets up the Pick squad write for <paramref name="stageId"/>; returns the requests it receives.</summary>
    public List<PickStageSquadRequest> SetupPickSquad(string stageId, Func<Task<IApiResponse>> respond)
    {
        var requests = new List<PickStageSquadRequest>();
        Api.Setup(x => x.PickStageSquadAsync(SeriesId, stageId, It.IsAny<PickStageSquadRequest>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string _, PickStageSquadRequest request, CancellationToken _) =>
            {
                requests.Add(request);
                return respond();
            });
        return requests;
    }

    public List<PickStageSquadRequest> SetupPickSquad(string stageId, IApiResponse response)
        => SetupPickSquad(stageId, () => Task.FromResult(response));

    /// <summary>A completed OK squads response.</summary>
    public IApiResponse<CollectionResponse<StageSquadSummaryResponse>> SquadsResponse(params StageSquadSummaryResponse[] squads)
        => Track(Stub(HttpStatusCode.OK, new CollectionResponse<StageSquadSummaryResponse> { Items = squads }));

    public void SetupSquads(string stageId, params StageSquadSummaryResponse[] squads)
        => SetupSquads(stageId, SquadsResponse(squads));

    public void SetupSquads(string stageId, IApiResponse<CollectionResponse<StageSquadSummaryResponse>> response)
        => Api.Setup(x => x.ListStageSquadsAsync(SeriesId, stageId, It.IsAny<CancellationToken>())).ReturnsAsync(response);

    public void SetupSquadsFailure(string stageId, HttpStatusCode status)
        => SetupSquads(stageId, Track(Stub<CollectionResponse<StageSquadSummaryResponse>>(status, null)));

    public void SetupSquadsPending(string stageId, TaskCompletionSource<IApiResponse<CollectionResponse<StageSquadSummaryResponse>>> completion)
        => Api.Setup(x => x.ListStageSquadsAsync(SeriesId, stageId, It.IsAny<CancellationToken>())).Returns(completion.Task);

    public void VerifySquadCalls(string stageId, Times times)
        => Api.Verify(x => x.ListStageSquadsAsync(SeriesId, stageId, It.IsAny<CancellationToken>()), times);

    public void VerifySeriesCalls(Times times)
        => Api.Verify(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>()), times);

    public void VerifyAdvancementCalls(Times times)
        => Api.Verify(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>()), times);

    /// <summary>The Stage card for <paramref name="stageId"/>.</summary>
    public static AngleSharp.Dom.IElement Card(IRenderedComponent<SeriesDetail> cut, string stageId)
        => cut.Find($"[data-testid='series-stage-card'][data-stage-id='{stageId}']");

    /// <summary>Presses the expander on a Stage card, so its output panel renders.</summary>
    public static async Task ExpandAsync(IRenderedComponent<SeriesDetail> cut, string stageId)
        => await Card(cut, stageId).QuerySelector("button.series-expander")!.ClickAsync();

    private static StubApiResponse<T> Stub<T>(HttpStatusCode status, T? content)
        where T : class
        => new()
        {
            IsSuccessStatusCode = (int)status is >= 200 and < 300,
            StatusCode = status,
            Content = content,
        };

    private StubApiResponse<T> Track<T>(StubApiResponse<T> response)
        where T : class
    {
        _disposables.Add(response);
        return response;
    }
}