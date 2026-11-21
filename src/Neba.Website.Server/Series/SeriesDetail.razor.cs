using System.Net;

using Microsoft.AspNetCore.Authorization;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Website.Server.Notifications;

using Refit;

namespace Neba.Website.Server.Series;

/// <summary>The Series page: the advancement rail for Series.EditSeries holders.</summary>
public partial class SeriesDetail
{
    private enum AdvancementState { Hidden, Loading, Loaded, Failed }

    private string? _advancementSeriesId;
    private bool _canView;
    private bool _canManage;
    private AdvancementState _advancementState = AdvancementState.Hidden;
    private SeriesAdvancementViewModel? _advancement;
    private readonly HashSet<string> _expandedStageIds = [];

    private bool ShowRail => _canView && _advancementState is AdvancementState.Loading or AdvancementState.Loaded;

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
        _expandedStageIds.Clear();
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
}