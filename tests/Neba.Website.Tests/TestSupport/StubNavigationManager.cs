using Microsoft.AspNetCore.Components;

namespace Neba.Website.Tests.TestSupport;

internal sealed class StubNavigationManager : NavigationManager
{
    public StubNavigationManager() => Initialize("https://localhost/", "https://localhost/");

    protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
}
