using System.Net;

using ErrorOr;

using Microsoft.AspNetCore.Authorization;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.Website.Server.Notifications;

using Refit;

namespace Neba.Website.Server.Series;

/// <summary>The Series page: the advancement rail for Series.EditSeries holders.</summary>
public partial class SeriesDetail
{
    private enum AdvancementState { Hidden, Loading, Loaded, Failed }

    private enum WriteOutcome { Saved, Conflict, Failed }

    private sealed record RecomputeTarget(string StageId, int Order, IReadOnlyList<int> OfficialOrders);

    private string? _advancementSeriesId;
    private bool _canView;
    private bool _canManage;
    private AdvancementState _advancementState = AdvancementState.Hidden;
    private SeriesAdvancementViewModel? _advancement;
    private int _version; // set by every successful advancement GET; every write sends it
    private bool _isWriting;
    private readonly HashSet<string> _expandedStageIds = [];

    private string? _squadStageId;
    private IReadOnlyList<StageSquadViewModel> _squads = [];
    private bool _isSquadsLoading;
    private string? _squadsError;

    private RecomputeTarget? _recomputeTarget;

    private bool ShowRail => _canView && _advancementState is AdvancementState.Loading or AdvancementState.Loaded;

    private StageAdvancementViewModel? SquadStage => _squadStageId is { } id ? _advancement?.StageById(id) : null;

    private SeriesStageViewModel? SquadDetail => _squadStageId is { } id ? _model?.Stages.FirstOrDefault(s => s.Id == id) : null;

    private string RecomputeTitle => _recomputeTarget is { } target ? SeriesDisplay.RecomputeTitle(target.Order) : string.Empty;

    private string RecomputeMessage => _recomputeTarget is { } target ? SeriesDisplay.OfficialResetMessage(target.OfficialOrders) : string.Empty;

    private void ResetAdvancementFor(string seriesId)
    {
        if (_advancementSeriesId == seriesId)
        {
            return;
        }

        _advancementSeriesId = seriesId;
        _advancement = null;
        _advancementState = AdvancementState.Hidden;
        _canView = false;
        _canManage = false;
        _version = 0;
        _expandedStageIds.Clear();
        _squadStageId = null;
        _recomputeTarget = null;
    }

    /// <summary>Checks the two permissions in code (an AuthorizeView cannot gate a fetch), then loads once.</summary>
    private async Task LoadAdvancementForViewerAsync()
    {
        if (_model is null)
        {
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var view = await AuthorizationService.AuthorizeAsync(authState.User, Permissions.EditSeries.PolicyName);
        _canView = view.Succeeded;
        if (!_canView)
        {
            _canManage = false;
            _advancementState = AdvancementState.Hidden;
            return;
        }

        var manage = await AuthorizationService.AuthorizeAsync(authState.User, Permissions.ManageAdvancement.PolicyName);
        _canManage = manage.Succeeded;

        if (_advancement is null)
        {
            _advancementState = AdvancementState.Loading;
            StateHasChanged(); // show the skeleton rows while the call runs
        }

        await LoadAdvancementAsync();
    }

    private async Task RetryAdvancementAsync()
    {
        _advancementState = AdvancementState.Loading;
        await LoadAdvancementAsync();
    }

    /// <summary>
    /// A reload keeps the old data on screen until the new data arrives. ApiExecutor cannot tell a 403
    /// from a 500, so the status is read inside the call.
    /// </summary>
    private async Task LoadAdvancementAsync()
    {
        HttpStatusCode? status = null;
        Func<CancellationToken, Task<IApiResponse<SeriesAdvancementResponse>>> call = async ct =>
        {
            var response = await SeriesApi.GetSeriesAdvancementAsync(Id, ct);
            status = response.StatusCode;
            return response;
        };

        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeriesAdvancement", call);

        if (result.IsError)
        {
            HandleAdvancementLoadError(status);
            return;
        }

        var response = result.Value;
        var localTimes = new Dictionary<DateTimeOffset, DateTimeOffset>();
        foreach (var utc in response.Instants())
        {
            localTimes[utc] = await ClientTimeZoneService.ToLocalAsync(utc);
        }

        _advancement = response.ToViewModel(new AdvancementAccess(_canView, _canManage), localTimes);
        _version = _advancement.Version;
        _advancementState = AdvancementState.Loaded;
    }

    private void HandleAdvancementLoadError(HttpStatusCode? status)
    {
        if (status == HttpStatusCode.Forbidden)
        {
            // The viewer lost the permission after the circuit opened: hide the section, no toast.
            _canView = false;
            _canManage = false;
            _advancement = null;
            _advancementState = AdvancementState.Hidden;
            return;
        }

        if (_advancement is not null)
        {
            ToastService.Show("Reload Failed", SeriesDisplay.AdvancementReloadFailed, NotifySeverity.Warning);
            return;
        }

        _advancementState = AdvancementState.Failed;
    }

    private void ToggleStage(string stageId)
    {
        if (!_expandedStageIds.Remove(stageId))
        {
            _expandedStageIds.Add(stageId);
        }
    }

    // ---- Settling a write ----

    /// <summary>
    /// 2xx: toast and reload. 409: toast the API's reason and reload (the caller's dialog closes). Anything else is
    /// <see cref="WriteOutcome.Failed"/>, and the caller shows it.
    /// </summary>
    private async Task<WriteOutcome> SettleWriteAsync(ErrorOr<Success> result, string successTitle, string successMessage)
    {
        if (!result.IsError)
        {
            ToastService.Show(successTitle, successMessage, NotifySeverity.Success);
            await LoadAdvancementAsync();
            return WriteOutcome.Saved;
        }

        if (result.FirstError.Type == ErrorType.Conflict)
        {
            ToastService.Show("Series Changed", SeriesDisplay.ConflictMessage(result.FirstError.Description), NotifySeverity.Warning);
            await LoadAdvancementAsync();
            return WriteOutcome.Conflict;
        }

        return WriteOutcome.Failed;
    }

    // ---- Recompute ----

    /// <summary>Runs at once when no Official Stage is in range; otherwise asks first, listing the Stages it resets.</summary>
    private async Task StartRecomputeAsync(StageAdvancementViewModel stage)
    {
        if (_isWriting || _advancement is null)
        {
            return;
        }

        var official = _advancement.OfficialOrdersFrom(stage.Order);
        if (official.Count == 0)
        {
            await RecomputeAsync(stage.Id, stage.Order, confirmed: false);
            return;
        }

        _recomputeTarget = new RecomputeTarget(stage.Id, stage.Order, official);
    }

    private Task ConfirmRecomputeAsync()
        => _recomputeTarget is { } target
            ? RecomputeAsync(target.StageId, target.Order, confirmed: true)
            : Task.CompletedTask;

    private void CancelRecompute()
    {
        if (!_isWriting)
        {
            _recomputeTarget = null;
        }
    }

    private async Task RecomputeAsync(string stageId, int order, bool confirmed)
    {
        if (_isWriting)
        {
            return;
        }

        _isWriting = true;
        try
        {
            var request = new RecomputeSeriesAdvancementRequest
            {
                Id = Id,
                StageId = stageId,
                Recomputation = new RecomputeSeriesAdvancementInput { Version = _version, ConfirmResetOfficial = confirmed },
            };

            var result = await ApiExecutor.ExecuteAsync("SeriesApi", "RecomputeSeriesAdvancement",
                ct => SeriesApi.RecomputeSeriesAdvancementAsync(Id, stageId, request, ct));

            _recomputeTarget = null;
            var outcome = await SettleWriteAsync(result, "Recomputed", SeriesDisplay.RecomputedMessage(order));

            if (outcome == WriteOutcome.Failed)
            {
                ToastService.Show("Recompute Failed", result.FirstError.Description, NotifySeverity.Error);
            }
            else if (outcome == WriteOutcome.Conflict && _advancement is not null)
            {
                // A Stage became official since the page loaded: ask again with the fresh list. ApiExecutor drops the
                // API's OfficialStageOrders metadata, so the list comes from the reloaded data.
                var official = _advancement.OfficialOrdersFrom(order);
                if (official.Count > 0)
                {
                    _recomputeTarget = new RecomputeTarget(stageId, order, official);
                }
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    // ---- Pick squad ----

    private async Task OpenSquadPickerAsync(StageAdvancementViewModel stage)
    {
        if (_isWriting)
        {
            return;
        }

        _squadStageId = stage.Id;
        _squads = [];
        _squadsError = null;
        _isSquadsLoading = true;

        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "ListStageSquads", ct => SeriesApi.ListStageSquadsAsync(Id, stage.Id, ct));
        _isSquadsLoading = false;

        if (result.IsError)
        {
            _squadsError = result.FirstError.Description;
            return;
        }

        _squads = [.. result.Value.Items.Select(s => s.ToViewModel())];
    }

    private void CloseSquadPicker() => _squadStageId = null;

    /// <summary>
    /// A 422 comes back to the dialog, which stays open. A 409 is handled here (toast and reload) and the dialog closes.
    /// Any other failure shows a toast and closes the dialog.
    /// </summary>
    private async Task<ErrorOr<Success>> SubmitSquadAsync(string squadId)
    {
        if (_squadStageId is not { } stageId)
        {
            return Result.Success;
        }

        _isWriting = true;
        StateHasChanged();
        try
        {
            var request = new PickStageSquadRequest
            {
                Id = Id,
                StageId = stageId,
                Squad = new PickStageSquadInput { SquadId = squadId, Version = _version },
            };

            var result = await ApiExecutor.ExecuteAsync("SeriesApi", "PickStageSquad",
                ct => SeriesApi.PickStageSquadAsync(Id, stageId, request, ct));

            var outcome = await SettleWriteAsync(result, "Squad Saved", SeriesDisplay.SquadSavedMessage);
            if (outcome != WriteOutcome.Failed)
            {
                return Result.Success;
            }

            if (result.FirstError.Type == ErrorType.Validation)
            {
                return result;
            }

            ToastService.Show("Pick Squad Failed", result.FirstError.Description, NotifySeverity.Error);
            return Result.Success;
        }
        finally
        {
            _isWriting = false;
        }
    }
}