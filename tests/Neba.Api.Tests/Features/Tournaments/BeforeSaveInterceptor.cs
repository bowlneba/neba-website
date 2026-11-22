using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Neba.Api.Tests.Features.Tournaments;

/// <summary>
/// Runs an action once, just before the context's first save. Tests use it to land a competing write between a
/// handler's checks and its save, which is how two concurrent requests collide on a unique index.
/// </summary>
internal sealed class BeforeSaveInterceptor(Func<Task> beforeSave) : SaveChangesInterceptor
{
    private bool _ran;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_ran)
        {
            _ran = true;
            await beforeSave();
        }

        return result;
    }
}
