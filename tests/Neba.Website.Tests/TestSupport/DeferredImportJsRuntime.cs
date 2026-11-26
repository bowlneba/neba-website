using Microsoft.JSInterop;

namespace Neba.Website.Tests.TestSupport;

// Holds the module import open until the test releases it.
internal sealed class DeferredImportJsRuntime : IJSRuntime, IJSObjectReference
{
    private readonly TaskCompletionSource<IJSObjectReference> _import = new();

    public List<string> Calls { get; } = [];

    public void CompleteImport() => _import.SetResult(this);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier == "import")
        {
            return new ValueTask<TValue>(ImportAsync<TValue>());
        }

        Calls.Add(identifier);
        return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

#pragma warning disable VSTHRD003 // The test completes this task itself.
    private async Task<TValue> ImportAsync<TValue>() => (TValue)await _import.Task;
#pragma warning restore VSTHRD003
}