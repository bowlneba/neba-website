# Backend learnings

Moved from CLAUDE.md. Read the relevant section before changing projections, logging and redaction, caching, EF collection navigations, or emails.

### Lightweight Collection Projections — Naming Convention

When a UI need (e.g. a picker/dropdown) only requires a reduced projection of an existing collection (a few scalar fields instead of the full aggregate graph), check whether an existing query already returns a superset of that data before adding a new query/endpoint. Reuse-and-project-down at the consuming layer is preferred over a parallel lightweight endpoint — e.g. a tournament-linking picker in the news create form reuses `ListTournamentsInSeasonQuery`/`ISeasonsApi.ListTournamentsInSeasonAsync` (already consumed via `ITournamentApiService.GetTournamentsForSeasonAsync` → `SeasonTournamentViewModel`) rather than adding a second, near-duplicate "just Id/Name/StartDate" query — the existing one already returns everything a picker needs, and a second query with the same route shape only invites drift between two sources of truth for the same data.

**If a genuinely new lightweight query/endpoint turns out to be justified** (the existing query is too expensive to call just for a picker, or scoped differently), follow this naming split so the "reduced projection" distinction lives in exactly one place:

- **The DTO/response type** is named with `Summary`/`Summaries` (e.g. `TournamentSummaryDto`, `TournamentSummaryResponse`) — this is the one place that signals "deliberately reduced fields."
- **The query, handler, and endpoint class names — and the route — stay named after the resource itself**, with no `Summary`/`Summaries` suffix (e.g. `ListTournamentsInSeasonQuery`, route `{seasonId}/tournaments`) — matching whatever the "full" operation for that resource would be named, not a variant name. Do not let the operation name double up on the same "reduced" signal the DTO name already carries.

This means a new lightweight query for an existing resource **cannot reuse the same class/route names as an existing heavier query for that resource** without a genuine rename of one of them — check for an existing `List{Resource}` query/endpoint first, since a collision here means either reusing the existing one (preferred, see above) or deliberately renaming the existing "full" variant to something more specific (a larger, higher-risk change touching its existing consumers) rather than inventing a suffix on the new one.

### PII Redaction in Logs

- Taxonomy: `Neba.Api.Compliance.DataTaxonomy` (`src/Neba.Api/Compliance/DataTaxonomy.cs`) — three `DataClassification`s: `Public` (not sensitive, no redaction), `Personal` (identifying but low-risk, partially masked), `Private` (sensitive PII, fully redacted). Extend this taxonomy rather than inventing a parallel one when a new category is needed.
- Attributes: `[PublicData]`, `[PersonalData]`, `[PrivateData]` (`Neba.Api.Compliance.*Attribute`, each wraps `DataClassificationAttribute` for its classification) — apply directly to any `[LoggerMessage]` parameter carrying a bowler's name/email/phone/address or similar. This is the whole convention: no manual masking helpers.
  - Use `[PrivateData]` for values that should never appear even partially (SSNs, payment info).
  - Use `[PersonalData]` for values that are useful to partially see for debugging/support (email addresses, names) — masked to first-character-plus-stars via `StarMaskingRedactor`.
  - Use `[PublicData]` only when you want to document that a parameter was deliberately reviewed and found non-sensitive (it's a no-op redaction-wise — `NullRedactor` passes the value through unchanged); omitting any attribute has the same runtime effect.
- Redactors registered per classification in `src/Neba.Api/Compliance/RedactionConfiguration.cs`, `AddRedaction()`: `NullRedactor` → `Public`, `StarMaskingRedactor` → `Personal` (custom, `src/Neba.Api/Compliance/StarMaskingRedactor.cs` — keeps the first character, stars out the rest), `ErasingRedactor` → `Private`. Called from `InfrastructureConfiguration.AddInfrastructure()`.
- **Gotcha — `builder.Services.AddRedaction(...)` alone does nothing.** It only registers `IRedactorProvider`/`IRedactor` in the container. The `[LoggerMessage]` source generator (`Microsoft.Gen.Logging`, from the `Microsoft.Extensions.Telemetry` package) emits code that reads `state.RedactedTagArray`, which is only populated when the logger itself is an `ExtendedLogger` — and that wrapper is only installed by calling **`builder.Logging.EnableRedaction()`** (from `Microsoft.Extensions.Telemetry`'s `LoggingRedactionExtensions`). Both calls are required; `AddRedaction()` in this codebase wires up both.
- Confirmed empirically: redaction applies to **both** the formatted `Message` string and the structured state tags (`FakeLogRecord.StructuredState`) — there's no separate code path to wire for Application Insights or other sinks, since they all consume the same `ILogger` state.
- `ErasingRedactor.Redact(...)` replaces the value with an **empty string**, not a placeholder token like `<redacted>`. E.g. a `[PrivateData]` parameter with value `"x@example.com"` produces an empty structured tag and an empty substitution in the formatted message.
- **`LoggerRedactionOptions.ApplyDiscriminator` (default `true`) folds the tag name into the value before redacting**, to prevent correlating redacted values across differently-named tags. This means a length-preserving redactor like `StarMaskingRedactor` produces more stars than the source value's own length (source + tag name length) — don't assert on an exact expected length; assert on the pattern instead (first char kept, rest starred, `ShouldNotContain` the original value/substrings).
- **Testing gotcha**: `FakeLogger<T>` constructed directly via `new FakeLogger<T>()` bypasses the DI logging pipeline entirely and never redacts anything, even with a classification attribute on the parameter — because it isn't wrapped by `ExtendedLogger`. Tests asserting on redaction must build a small DI container instead: `new ServiceCollection().AddLogging(l => l.AddFakeLogging().EnableRedaction()).AddRedaction(...).BuildServiceProvider()`, then resolve `ILogger<T>` and `IServiceProvider.GetFakeLogCollector()` from it. See `GoogleWorkspaceEmailSenderTests.SendAsync_ShouldMaskRecipientAddress_InFormattedMessageAndStructuredState` for the pattern. Tests that don't assert on log content (e.g. constructed with a plain `new FakeLogger<T>()`) are unaffected and don't need this.
- `RefitSettings.ExceptionRedactor` in `src/Neba.Website.Server/Services/ApiServicesConfiguration.cs` is an unrelated, pre-existing HTTP-header-scrubbing mechanism — do not confuse it with this feature despite the similar name.

### FusionCache Deserialization Recovery

- Cached query DTOs should use serialization-safe types; do not store domain `SmartEnum` instances directly in cached DTO properties.
- Map SmartEnum values to primitives in query projections (for example, `Status.Name` as `string`) before caching.
- `CachedQueryHandlerDecorator` catches cache deserialization failures on plain cached queries, logs a warning, executes the inner handler, and rewrites the cache entry.
- Keep the cache key stable unless explicitly directed otherwise; deserialization fallback handles stale entry recovery.

### EF Core Navigation Fixup — `= []` Collection Initializers Cause `Collection was of a fixed size`

When a domain entity initializes a collection navigation property with `= []` (C# 12 collection expression), the CLR resolves `IReadOnlyCollection<T> Prop { get; init; } = []` to `T[]` (a fixed-size array). EF Core 10's `ClrCollectionAccessorFactory` picks up this array type as `TCollection`, and when navigation fixup tries to call `AddStandalone(array, value)`, it hits `SZArrayHelper.Add` which throws `System.NotSupportedException: Collection was of a fixed size`.

This affects **both sides** of a relationship: adding a `TournamentSponsor` with a concrete `SponsorId` set causes EF to fix up `Sponsor.TournamentsSponsored` (also `= []`), even if you never set `Tournament = tournament` on the dependent.

**Symptom**: `NotSupportedException: Collection was of a fixed size` in the EF Core navigation fixup stack during integration test seeding.

**Fix in tests**: After saving the principal entities, call `_dbContext.ChangeTracker.Clear()` before adding dependent entities. With no tracked principals in the change tracker, EF has nothing to fixup against.

```csharp
await _dbContext.SaveChangesAsync(ct);

var tournamentDbId = _dbContext.Entry(tournament)
    .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

_dbContext.ChangeTracker.Clear(); // prevents fixup against tracked sponsors/tournaments

var ts = _dbContext.Set<TournamentSponsor>().Add(new TournamentSponsor { SponsorId = sponsorId, ... });
ts.Property<int>(TournamentConfiguration.ForeignKeyName).CurrentValue = tournamentDbId;

await _dbContext.SaveChangesAsync(ct);
```

**Note**: `PropertyAccessMode.Field` / `Navigation().HasField("_sponsors")` does NOT help — EF still determines `TCollection` from the property type, not the backing field type.

**Ordering constraint when combining TournamentSponsors + other dependents in the same test**: Any entities added via navigation properties to already-saved aggregates (e.g. `HistoricalTournamentChampion { Tournament = tournament }`) must be added and saved **before** `ChangeTracker.Clear()`. After the clear, detached entities passed as navigation properties are re-tracked as `Added`, causing a unique constraint violation on re-insert. The required save order for a fully-populated tournament test is:

1. Save all principals (season, bowling center, tournament, sponsors, bowlers)
2. Add `HistoricalTournamentChampion` entries (tournament + bowlers still tracked) → `SaveChangesAsync`
3. Read `tournamentDbId` from shadow property
4. `ChangeTracker.Clear()`
5. Add `TournamentSponsor` entries via shadow FK → `SaveChangesAsync`

**Stable Verify snapshots for tournaments**: Use explicit IDs via the source-generated `TournamentId(string)` constructor (the `ulid-full.typedid` template generates `public PLACEHOLDERID(string value)`). All-numeric ULID strings are valid (e.g. `"01000000000000000000000001"`). Apply the same to `SeasonId`, `BowlerId`, `SponsorId` — any ID that will appear in the snapshot output.

**Root-cause fix, preferred over the `ChangeTracker.Clear()` workaround above**: the `ChangeTracker.Clear()` workaround only helps when a test controls exactly when fixup runs against a *write*. It does nothing for the *read* path — any query that materializes the owner entity directly (e.g. `.SingleAsync(...)` returning the full `Sponsor`) triggers the same fixup when EF auto-includes the owned collection, throwing even with zero writes involved, as long as the collection is non-empty. The actual fix is to stop the property from ever holding an array:

- **Auto-property default** (`{ get; init; } = []`) → initialize with `new List<T>()` instead: `public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; init; } = new List<PhoneNumber>();`
- **Any fallback assigned into that property** (e.g. `PhoneNumbers = phoneNumbers ?? []` in a `Create()` factory or test factory) needs the same treatment: `phoneNumbers ?? new List<PhoneNumber>()`.
- Prefer the **backing-field pattern** already used by `Tournament._sponsors`/`_articles`/`_oilPatterns` and `SideCut._criteriaGroups` for any collection navigation that also needs mutator methods on the aggregate: `private readonly List<T> _field = [];` (here `[]` is fine — it's target-typed to the concrete `List<T>` field, not an interface, so the compiler doesn't synthesize an array) with `public IReadOnlyCollection<T> Prop => _field;`.

Confirmed and fixed for `Sponsor.PhoneNumbers`, `Sponsor.TournamentsSponsored`, and `BowlingCenter.PhoneNumbers` (all three were `{ get; init; } = []` auto-properties), plus the matching `?? []` fallbacks in `Sponsor.Create()`, `SponsorFactory.Create()`, and `BowlingCenterFactory.Create()`. After the fix, direct materialization of the owner entity with a populated collection (no `ChangeTracker.Clear()`, no projection workaround) works normally — see `CreateSponsorCommandHandlerTests.HandleAsync_ShouldPersistPhoneNumbers_WhenProvided`.

**This fix will not survive `dotnet format`/SonarQube unattended** — `dotnet_diagnostic.IDE0305.severity = warning` in `.editorconfig` ("Simplify collection initialization") actively suggests turning `new List<T>()` back into `[]`, and the pre-push Husky hook runs `dotnet format` and auto-commits any changes it makes. **IDE0028** ("Simplify collection initialization" — the non-target-typed sibling rule, also set to `warning` in `.editorconfig`) makes the identical suggestion and must be suppressed alongside IDE0305 at every one of these sites; suppressing only IDE0305 leaves IDE0028 free to flag (and `dotnet format` free to auto-fix) the same line. Every site fixed above therefore needs **both** of the following, not just one:

1. A `[SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "...")]` **and** a matching `[SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "...")]` on the containing property/method, with the justification (or a preceding `//` comment) explaining the fixed-size-array hazard — see `Sponsor.PhoneNumbers`, `Sponsor.TournamentsSponsored`, `Sponsor.Create()`, `BowlingCenter.PhoneNumbers`, `SponsorFactory.Create()`, `BowlingCenterFactory.Create()` for the pattern.
2. A regression test that casts the default collection instance to `ICollection<T>` and asserts `Add` doesn't throw (`Should.NotThrow(() => mutable.Add(...))`) — this is the only check that actually fails if someone (or a tool) reverts the fix, since a plain equality/count assertion can't distinguish a `List<T>` from a `T[]` with the same contents. See `SponsorTests.PhoneNumbers_DefaultInstance_ShouldSupportAdd_ForEfFixup` / `PhoneNumbers_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup` / `TournamentsSponsored_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup` and `BowlingCenterTests.PhoneNumbers_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup`. Verified by manually reverting `BowlingCenter.PhoneNumbers` to `[]` and confirming the test fails with `NotSupportedException`, then restoring it.

**When introducing a new `= []`-defaulted `IReadOnlyCollection<T>`/`IEnumerable<T>`/`IReadOnlyList<T>` navigation property on any EF-mapped entity** (owned collection or `HasMany`), apply both of the above immediately rather than waiting to hit the exception — the backing-field pattern (point 3 above) sidesteps the whole problem and needs neither suppression nor this style of test, so prefer it for any new collection navigation that also needs mutator methods.

### Email Template Pattern

- Each email is an `internal sealed class` in `{Feature}/Emails/{Name}Email.cs` — primary constructor takes email-specific values, exposes `ToHtmlBody()`.
- `EmailLayout.Wrap(innerHtml)` in `Neba.Api.Email` provides the branded chrome: NEBA blue (`#1a3a6e`) header with logo, white content area, and gray footer.
- The NEBA logo is served as a hosted URL (`https://bowlneba.com/images/neba-logo.png`) — **never use base64-embedded images** in email. Gmail Desktop/iOS/Android and many other clients have zero support for base64 images and strip them entirely. The `Email/Resources/neba-logo.png` embedded resource is no longer used.
- **Use inline styles only** — Gmail strips `<style>` blocks, so all styles must be on the elements themselves.
- **Always `WebUtility.HtmlEncode` user-supplied values** (links, codes) before embedding them in `href` attributes and visible text — prevents broken HTML when the value contains `&`, and guards against injection.
- Brand constants: header/button bg `#1a3a6e`, body text `#444`, muted/footer `#999`, page bg `#e8e8e8`.
- **Adding a new email**: create `{Feature}/Emails/{Name}Email.cs`, take constructor params, call `EmailLayout.Wrap(...)` in `ToHtmlBody()`. No infrastructure changes needed.
- **Mock-verification tests** (`IdentityEmailSenderAdapterTests`): always add `.Verifiable()` to the `Setup` and call `_sender.VerifyAll()` in the Assert block — the SonarAnalyzer (S2699) requires at least one explicit assertion per test.

#### Email HTML Compatibility Rules (from caniemail.com audit)

- **Use `<table>` for layout, not `<div>`** — `max-width` and `margin:0 auto` centering on `<div>` elements don't work in Outlook Windows. Use nested `<table role="presentation">` with `width` attribute and `align="center"` on the outer `<td>` instead.
- **Never use `overflow:hidden`** — only 54% email client support. For rounded corners on the outer container, accept that corners will be square in most clients (cosmetic only).
- **Logo image must be `display:block;margin:0 auto`** — `display:inline-block` has only 57% support (Outlook Windows doesn't support it except `display:none`). `display:block` is safe; centering within a `text-align:center` cell works everywhere.
- **`border-radius` is cosmetic only** — 64% support; buttons and boxes lose rounded corners in Outlook Windows and others. Acceptable degradation.
- **`<body>` has only 40% full support** — 34% of clients strip it entirely (Outlook Windows, Apple Mail, Samsung Email); another 26% replace it with a `<div>`. Any styles on `<body>` (background color, font-family) must be duplicated on the outer `<table>` as a fallback. Background color AND font-family both need to be on the outer table, not just the body.
- **`text-align:center`** is safe; avoid flow-relative values (`start`, `end`) which have ~38% less support.

