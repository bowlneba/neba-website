# Migrate Tournaments, Sponsors, and News to the shared upload pieces

The Hall of Fame photo feature (#215) added generic upload helpers in `src/Neba.Api/Uploads/`. Tournaments, Sponsors, and News still use their own copies. This plan moves them over in a tech-debt branch, opened right after the Hall of Fame photo work merges to the release branch.

No schema change is needed. The pending-upload table, the stored-file columns, and the blob paths stay as they are. `dotnet ef migrations add` should produce nothing; if it does, something other than this refactor changed the model.

## What exists now

| Shared (in `Neba.Api.Uploads`) | Replaces |
| --- | --- |
| `StoredFileClaim` (`Resolve`, `EnsureClaimableAsync`, `IsSameFile`) | `TournamentLogoClaim` |
| `PendingUploadCleaner` | `TournamentPendingUploadCleaner`, `SponsorPendingUploadCleaner` |
| `StoredFileCleanup` | `TournamentLogoCleanup` |
| `DeleteStoredFilesJob`, `BlobReference`, `DeleteStoredFilesJobHandler` | `DeleteTournamentFilesJob` + `TournamentFileReference`, `DeleteSponsorFilesJob` + `StoredFileReference`, `DeleteArticleFilesJob` |

`StoredFileClaim.EnsureClaimableAsync` takes the "not uploaded" error as a parameter, so each feature keeps its own error code and message.

## Steps

Do one feature per commit. In each commit, switch the callers, delete the old classes, and port the tests, so no commit has two live copies.

1. **Tournaments**
   - Callers: `CreateTournamentCommandHandler`, `EditTournamentCommandHandler`, `EditTournamentLogoCommandHandler`, `DeleteTournamentCommandHandler`, and `TournamentLogoResolver` (check whether it uses the claim).
   - Replace `TournamentLogoClaim.EnsureClaimableAsync(db, logo, current, ct)` with `StoredFileClaim.EnsureClaimableAsync(db, logo, current, TournamentErrors.LogoNotUploaded, ct)`.
   - Replace `TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo` with `StoredFileCleanup.ScheduleDeleteOfReplaced`.
   - Replace `TournamentPendingUploadCleaner.RemoveClaimedAsync` with `PendingUploadCleaner.RemoveClaimedAsync`.
   - `DeleteTournamentCommandHandler` enqueues `DeleteTournamentFilesJob` with every file the tournament owns; switch it to `DeleteStoredFilesJob` with `BlobReference`.
   - Delete `TournamentLogoClaim`, `TournamentLogoCleanup`, `TournamentPendingUploadCleaner`, `DeleteTournamentFilesJob`, `DeleteTournamentFilesJobHandler`, `TournamentFileReference`, and their tests (the shared tests already cover the same cases).
2. **Sponsors**
   - Callers: `CreateSponsorCommandHandler`, `EditSponsorCommandHandler`.
   - Replace `SponsorPendingUploadCleaner` and `DeleteSponsorFilesJob` + `StoredFileReference`.
   - Delete the old classes and port the handler tests.
3. **News**
   - Callers: `DeleteArticleCommandHandler`, `CreateArticleCommandHandler`, `EditArticleCommandHandler`.
   - Replace `DeleteArticleFilesJob` and its handler. Check whether the article handlers claim uploads through the same pattern, and use `StoredFileClaim` and `PendingUploadCleaner` if they do.
4. **Hangfire.** Job type names change (`DeleteTournamentFilesJob`, `DeleteSponsorFilesJob`, `DeleteArticleFilesJob` become `DeleteStoredFilesJob`). Hangfire stores jobs by type name, so a job queued by the old release and read by the new one would fail.
   - Before deploying, check the Hangfire dashboard for queued, scheduled, or retrying jobs of the old types.
   - Delete jobs run right after an edit, so the window is small.
   - If any are queued, deploy after they finish, or keep the old job classes for one release.
5. **Check.** Run the Tournaments, Sponsors, News, and Uploads components (`dotnet test --filter-trait "Component=..."`), then the full API test project.

## Out of scope

- Moving the `elm-*` modal styles shared by the tournament logo and Hall of Fame photo modals into one partial.
- Any change to blob paths or containers.
