using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Neba.Website.Server.Notifications;

namespace Neba.Website.Tests.TestSupport;

internal static class BunitContextExtensions
{
    /// <summary>
    /// Baseline setup shared by every test that renders <see cref="Neba.Website.Server.Documents.NebaDocument"/>
    /// (directly, or via a hosting page like TournamentRules/Bylaws). Registers a default unauthenticated
    /// authorization context (so the AuthorizeView-gated refresh button doesn't throw for tests that don't
    /// care about it) and a real ToastService (so NebaDocument's @inject resolves) — callers that need to
    /// assert on a specific permission or toast content override these afterward.
    /// </summary>
    internal static void SetupNebaDocumentModule(this BunitContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule("./Documents/NebaDocument.razor.js");

        context.AddAuthorization().SetNotAuthorized();

        // Registered by type (not instance) so the container owns construction/disposal —
        // ToastService is IDisposable and BunitContext disposes container-created singletons
        // when the test's context is disposed.
        context.Services.AddSingleton<ToastService>();
    }
}