using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Counts the saves that reach the database through one DbContext.</summary>
internal sealed class SaveCounter : SaveChangesInterceptor
{
    public int Saves { get; private set; }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Saves++;

        return ValueTask.FromResult(result);
    }
}

/// <summary>Runs an action once, just before the first save, to model another writer that commits between a load and a save.</summary>
internal sealed class BeforeSaveInterceptor(Func<CancellationToken, Task> action) : SaveChangesInterceptor
{
    private bool _ran;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_ran)
        {
            _ran = true;
            await action(cancellationToken);
        }

        return result;
    }
}