---
name: new-background-job
description: Add a Hangfire background job to Neba.Api - the job record, its handler, enqueueing or recurring schedule, serialization and retry traps, and tests. Use when work must run outside the request (file cleanup, sync, notification) or on a schedule. Not for a one-off data port (add-ef-migration, data-migration/*.linq) or a domain event on its own (see CLAUDE.md, IDomainEventPublisher).
---

# New background job

Read `docs/learnings/hangfire-postgres.md` first. The `backend-api` agent does this work; this is the procedure.

## Copy this

| Need | Copy from `src/Neba.Api/` |
|---|---|
| Job record and handler | `Features/Sponsors/EditSponsor/DeleteSponsorFilesJob.cs` and `DeleteSponsorFilesJobHandler.cs` |
| Enqueue from a handler | `Features/Sponsors/EditSponsor/EditSponsorCommandHandler.cs` (`IBackgroundJobScheduler.Enqueue`) |
| Recurring job | `Features/Seasons/SeasonsConfiguration.cs` (`AddOrUpdateRecurring` with a cron string) |
| Handler test | `tests/Neba.Api.Tests/Features/Sponsors/EditSponsor/DeleteSponsorFilesJobHandlerTests.cs` |
| Serialization test | `tests/Neba.Api.Tests/BackgroundJobs/DeleteFilesJobSerializationTests.cs` |

## Steps

1. **Job.** A `public sealed record` implementing `IBackgroundJob` (`JobName` is shown in the dashboard and logs). Keep it small: ids and values, not entities.
2. **Handler.** An `internal sealed class` implementing `IBackgroundJobHandler<TJob>` with `ExecuteAsync(job, ct)`. Handlers register by scanning (`BackgroundJobConfiguration`), so add no DI line. Inject services; a job runs in its own scope.
3. **Trigger.** Inject `IBackgroundJobScheduler`. Use `Enqueue` for work now, `Schedule` for a delay, `EnqueueOnce` with a deduplication key to avoid doubles, `AddOrUpdateRecurring` for a cron schedule. Enqueue after `SaveChangesAsync` succeeds, as `EditSponsorCommandHandler` does. A recurring job is registered at startup from an extension on `WebApplication`, like `UseSeasonRolloverJob`.
4. **Failure.** Decide what a retry does. A handler that loops over items should log and continue on one item's failure (see `DeleteSponsorFilesJobHandler`), and always log a caught exception. `[NoAutomaticRetry]` and `OptOutAutomaticRetry` exist in `BackgroundJobs/`; read them before you use either.
5. **Logging.** `[LoggerMessage]` methods. Put `[PersonalData]` or `[PrivateData]` on any parameter that carries personal data.
6. **Tests.** A handler test with `FakeLogger<T>` for log paths, and a serialization round-trip test for any job with a collection.

## Traps

- **Collection properties.** Hangfire's JSON records the runtime type and cannot rebuild the compiler-generated type a collection expression makes. Copy into a `List<T>` in the setter, as `DeleteSponsorFilesJob.Files` does (with its three `SuppressMessage` attributes). Test it.
- `EnableTransactionScopeEnlistment` stays `false` in `BackgroundJobConfiguration.cs`. Do not turn it on.
- **Dev runs one worker** (`Hangfire:WorkerCount: 1`), so one wedged job stops every local job. Production runs three.
- A job that touches the legacy database or ported data follows "Software data migration" in `CLAUDE.md`.

## Checklist

- [ ] Job record, handler, trigger
- [ ] Collections copied to `List<T>` and a serialization test
- [ ] Failure and retry decided and logged
- [ ] PII attributes on log parameters
- [ ] Handler test, and a scheduling test if recurring
