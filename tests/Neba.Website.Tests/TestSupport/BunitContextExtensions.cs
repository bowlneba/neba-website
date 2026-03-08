using Bunit;

using Microsoft.JSInterop;

namespace Neba.Website.Tests.TestSupport;

internal static class BunitContextExtensions
{
    internal static void SetupNebaDocumentModule(this BunitContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule("./Documents/NebaDocument.razor.js");
    }
}
