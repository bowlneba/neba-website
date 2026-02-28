# NEBA Website - Ubiquitous Language

> **Purpose**: Defines the common vocabulary used throughout the NEBA Website for business domain concepts. These terms are used consistently in code, documentation, and user interfaces to ensure clear communication between developers, stakeholders, and users.

---

## How to Use This Document

**For Everyone**: These are the business concepts and terms we use when discussing the NEBA Website. Whether you're a developer, board member, or user, we all use these same terms to mean the same things.

**For Developers**: Use these exact terms in code (class names, method names), comments, and discussions. When a business person says "document," you should immediately know what they mean.

**For AI Assistants**: Always use these terms as defined here. These are non-negotiable domain concepts.

**Living Document**: As we add new features, we'll add new terms here. This grows with the project.

---

## Content Management

### Document

**Definition**: Content sourced from external document management systems (Google Drive, Microsoft Office 365) that represents organizational policies, rules, and reference material.

**Characteristics**:

- **Source**: Google Drive (Google Docs, Sheets, Slides, etc.)
- **Purpose**: Organizational content (bylaws, rules, policies, reference guides)
- **Format**: Exported as HTML for web display
- **Storage**: Cached in Azure Blob Storage for performance
- **Access Pattern**: Infrequent updates (monthly scheduled syncs), high read volume
- **Lifecycle**: Long-lived, version-controlled by source system

**Examples**:

- NEBA Bylaws
- Tournament Rules
- Officer Handbook
- Tournament Director Guide
- Scoring Guidelines

**In Code**:

- Namespace: `Neba.Application.Documents`
- Interface: `IDocumentsService`
- Models: `DocumentDto`, `DocumentViewModel`

---

### File

**Definition**: Any content stored in Azure Blob Storage. Files are the storage representation of content within NEBA, regardless of origin. A file may be a cached export of a Document (e.g., bylaws HTML from Google Drive), an uploaded artifact (e.g., tournament squad results PDF, scanned recap sheets), or any other binary/text content. Files may or may not be associated with a domain entity (tournament, bowler, bowling center). Each file lives in a container with a path and carries metadata describing its content type and provenance.

**Characteristics**:

- **Storage**: Azure Blob Storage (Azurite locally)
- **Purpose**: Persistent storage for any content — cached documents, uploaded artifacts, generated reports
- **Metadata**: Each file carries key-value metadata (content type, provenance, timestamps)
- **Lifecycle**: Varies by use case — cached documents are refreshed periodically, uploaded artifacts are long-lived

**Examples**:

- Cached bylaws HTML exported from Google Drive
- Tournament squad results PDF
- Scanned recap sheets from tournaments

**In Code**:

- Namespace: `Neba.Application.Storage`
- Interface: `IFileStorageService`

---

### Document Refresh

**Definition**: The process of synchronizing a Document from its source system (Google Drive) to the application's cache layers.

**Process Flow**:

1. Triggered by scheduled job or manual user action
2. Export document from Google Drive as HTML
3. Process HTML (clean formatting, update links)
4. Upload to Azure Blob Storage (as a File)
5. Invalidate application caches

**Types**:

- **Scheduled Refresh**: Automatic monthly sync (configurable per document)
- **Manual Refresh**: User-triggered via admin interface
- **Force Refresh**: Bypasses all caches and re-exports from source

**Status States**:

- `Retrieving` - Exporting from Google Drive
- `Uploading` - Saving to Azure Blob Storage
- `Completed` - Successfully synced
- `Failed` - Error occurred during sync

**In Code**:

- Command: `RefreshDocumentCacheCommand`
- Job: `SyncHtmlDocumentToStorageJob`
- Event: `DocumentRefreshStatusEvent`

---

## Contact

### Address

**Definition**: The physical postal location of a NEBA entity (bowling center, bowler). Captures street, unit, city, state or province, country, and postal code.

**Characteristics**:

- **Country scope**: US and Canada. Bowling centers are always US; bowlers may be US or Canadian
- **Default country**: US
- **Postal codes**: Normalized on input — dashes and spaces stripped internally (`12345-6789` → `123456789`; `K1A 0B1` → `K1A0B1`)
- **Coordinates**: Optional on Address itself. Whether coordinates are required is determined by the owning entity (e.g., BowlingCenter), not by Address
- **No edge cases in scope**: P.O. boxes, rural routes, and military addresses are not supported

**Fields**:

| Field | Required | Notes |
| --- | --- | --- |
| Street | Yes | e.g., `123 Main St` |
| Unit | No | Suite, apt, or lane number |
| City | Yes | |
| Region | Yes | `UsState` (US) or `CanadianProvince` (CA) — internal code term, never user-facing |
| Country | Yes | `US` or `CA`; defaults to `US` |
| PostalCode | Yes | US: `12345` or `12345-6789`; CA: `A1A 1A1` |
| Coordinates | No | Set by geocoding, never entered manually |

**UI Note**: The `Region` field label renders as **State** when country is US, and **Province** when country is Canada. The term *Region* is an internal code concept and is never shown to users.

**In Code**:

- Namespace: `Neba.Domain.Contact`
- Type: `Address` (sealed record)
- Related enums: `UsState`, `CanadianProvince` (SmartEnums)

---

### Phone Number

**Definition**: A contact telephone number associated with a NEBA entity (bowling center or bowler). Phone numbers are North American (NANP) only, consistent with the address scope of the system. Staff refer to this simply as "phone number."

**Characteristics**:

- **Format**: Stored as raw digits only — formatting characters (dashes, parentheses, spaces, dots) are stripped on input. `"(203) 555-1234"` → stored as `"2035551234"`
- **Display format**: `(203) 555-1234` — always rendered in this format on screen
- **Country code**: Always `"1"` (NANP). Non-North American numbers are not in scope
- **Extensions**: Optional digits-only suffix. Applicable to both bowling centers and bowlers (e.g., a work number at a corporate-owned center, or a bowler's work number). Displayed as `ext. 123`
- **No phone on file**: The absent state — used when no phone number has been recorded for a bowler. Never applicable to a bowling center (see Business Rules)
- **History**: Last-write-wins. Phone number changes are not tracked

**Phone Number Types**:

| Type | Description |
| --- | --- |
| Work | The primary publicly listed number (e.g., the number you would find looking up the entity) |
| Home | Residential number — bowlers only |
| Mobile | Cell phone number — bowlers only |
| Fax | Facsimile number |

**Bowling Center Phone Numbers**:

- **Work**: Required. The publicly listed number for the center. May include an extension (e.g., for centers owned by a corporate chain where the main line requires an extension to reach the center directly)
- **Fax**: Optional. May include an extension
- Home and Mobile types are not applicable to bowling centers

**Bowler Phone Numbers**:

- Multiple phone numbers are supported across any combination of types
- No phone number is required — a bowler may have none on file
- Extensions are applicable to work numbers
- "No phone on file" is the correct term when no phone number has been recorded

**Validation**:

- Area code first digit must be 2–9
- N11 codes (211, 311, 411 … 911) are rejected — these are reserved service codes, not valid area codes
- Toll-free numbers (800, 888, 877, 866, 855, 844, 833) are supported — bowling centers may use a corporate toll-free number as their contact number
- Structurally valid format only — no validation beyond format (e.g., no SMS-capability check)

**Business Rule**: A bowling center must always have a work phone number on file.

**In Code**:

- Namespace: `Neba.Domain.Contact`
- Type: `PhoneNumber` (sealed record)
- Factory: `PhoneNumber.CreateNorthAmerican(number, extension?)`

---

### Email Address

**Definition**: An electronic mail address associated with a NEBA entity (bowling center or bowler). Staff refer to this as "email address" or simply "email."

**Characteristics**:

- **Format**: Stored and displayed as-is — no normalization applied
- **Validation**: Standard email format validation (must contain `@` with a valid structure). Structurally valid format only — no mailbox existence check is performed
- **One per entity**: A single email address is supported per entity. No type distinction (home, work, etc.) is needed
- **No email on file**: The correct term when no email address has been recorded for an entity

**Bowling Center Email**:

- Optional — most centers have one but it is not required

**Bowler Email**:

- Optional — no email address is required
- Email address is a potential future notification channel. Bowler opt-in preference ("receive emails") is a separate concern managed on the Bowler entity, not here

**Business Rule**: No entity is required to have an email address on file.

**In Code**:

- Namespace: `Neba.Domain.Contact`
- Type: `EmailAddress` (sealed record)

---

## Geography

### Coordinates

**Definition**: A geographic point expressed as latitude and longitude in WGS84 decimal degrees. Coordinates are an internal concept used to enforce the 35-mile rule. They are never labeled or presented to users.

**Characteristics**:

- **Latitude**: −90 to 90
- **Longitude**: −180 to 180
- **Source**: Geocoded via Azure Maps; never entered manually by staff or users
- **Lifecycle**: Cleared and re-geocoded whenever the owning entity's address is updated
- **Not user-facing**: The term *Coordinates* does not appear in any UI; it has no public label

**Usage**:

- `BowlingCenter` must always have Coordinates — enforced as an aggregate invariant, not on Address
- `Address` may exist without Coordinates; whether Coordinates are required depends on the owning aggregate
- Used exclusively to calculate distances for the 35-mile rule

**In Code**:

- Namespace: `Neba.Domain.Geography`
- Type: `Coordinates` (sealed record)

---

## Bowling Centers

### Certification Number

**Definition**: A numeric identifier issued by the United States Bowling Congress (USBC) to a bowling center whose lanes have been inspected and confirmed to meet USBC standards for sanctioned competition. Certification is tied to the bowling center's physical address. As a USBC requirement, sanctioned competition may only be held at certified bowling centers; NEBA, as a USBC-sanctioned tournament organization, is bound by this requirement.

**Characteristics**:

- **Issuer**: USBC
- **Format**: Numeric only; up to 5 digits (no enforced minimum — values as short as 2 digits exist in USBC records)
- **Storage**: Stored as a string — no arithmetic is performed on this value
- **Leading zeros**: Not semantically significant. `"01948"` and `"1948"` refer to the same certification. Formatting varies by USBC context
- **Source**: Sourced directly from USBC data — format is accepted as-is
- **Lifecycle**: Assigned once by USBC and does not change. A bowling center that is destroyed and rebuilt — even under the same name — is treated as a new bowling center with a new certification number, not an update to the existing record. The USBC API import is the authority on this distinction: a new certification number means a new center
- **Decertification**: Not modeled — not a real-world concern for NEBA's operations

**Placeholder Values**:

The USBC API only returns open (active) bowling centers. NEBA has a 60-year history that includes centers which have since closed and whose certification numbers are no longer retrievable from USBC. These centers are represented with an internal placeholder value prefixed with `x` (e.g., `x001`). Placeholders are an internal system concept only — users never see them. The certification number field is blank in the UI for any center with a placeholder value. If the actual certification number is ever identified, the placeholder is replaced accordingly.

**Business Rule**: A bowling center without a known certification number cannot host sanctioned competition.

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `CertificationNumber` (sealed record)
- Factory: `CertificationNumberFactory`

**Code Validation**:

- `Create` rejects non-numeric input (placeholders are handled separately via factory)
- No length invariant — USBC data varies (2–5 digits observed)
- Leading zeros stored as received from USBC; no normalization applied

---

### BowlingCenter

**Definition**: A physical bowling facility certified by the United States Bowling Congress (USBC) and tracked by NEBA for tournament hosting and membership purposes. BowlingCenter is the aggregate root for all bowling center concepts.

**Characteristics**:

- **Identity**: Uniquely identified by its Certification Number — not by name, ownership, or address. A surrogate database key exists for persistence but carries no domain meaning
- **Scope**: US only
- **Name**: The current publicly known operating name. Mutable — updated in place on rebrand or ownership change. NEBA does not track historical names
- **Website**: The center's public website URL. Optional. Validated as a well-formed URI on import and update. Informational only — no domain behavior

**Status transitions**:

- A center may transition from `Closed` back to `Open` if USBC re-certifies the same physical location under the same Certification Number (e.g., new ownership)
- If a location reopens under a new Certification Number, the original record remains `Closed` and a new BowlingCenter is created at the same address

**Business Rules**:

- Must always have a Work phone number on file
- Must always have Coordinates — enforced as an aggregate invariant. A center without Coordinates cannot be created; geocoding failures during import require manual intervention before the record is committed
- A center with a placeholder Certification Number cannot host sanctioned competition

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `BowlingCenter` (aggregate root)

---

### BowlingCenterStatus

**Definition**: An enumeration representing the operational state of a Bowling Center.

| Value | Meaning |
| --- | --- |
| `Open` | The center is active and available for tournament hosting and league play |
| `Closed` | The center is no longer operating |

> The enumeration is intentionally extensible — future values such as `TemporarilyClosed` or `PendingCertification` may be added without a schema change.

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `BowlingCenterStatus`

---

### WebsiteId

**Definition**: The integer identifier used by the existing NEBA website to reference a Bowling Center. Nullable — only present for centers that existed in the legacy website's database.

**Characteristics**:

- **Type**: Nullable integer
- **Source**: Legacy website database — assigned by that system, not by NEBA staff
- **Purpose**: Data migration only. Allows records imported from the legacy website to be traced back to their origin, and enables cross-referencing during the migration period
- **Lifecycle**: Temporary. Once the new application (this system) goes live and the legacy website is retired, this field is no longer needed and should be dropped

**In Code**:

- Property: `BowlingCenter.WebsiteId` (`int?`)

---

### LegacyId

**Definition**: The integer identifier used by the existing NEBA WinForms application to reference a Bowling Center. Nullable — only present for centers that existed in the WinForms application's database.

**Characteristics**:

- **Type**: Nullable integer
- **Source**: WinForms application database — assigned by that system, not by NEBA staff
- **Purpose**: Data migration only. Allows records imported from the WinForms application to be traced back to their origin, and enables cross-referencing during the porting period
- **Lifecycle**: Temporary. Once all WinForms functionality has been ported to this application and the WinForms application is sunset, this field is no longer needed and should be dropped

**In Code**:

- Property: `BowlingCenter.LegacyId` (`int?`)

---

### LaneConfiguration

**Definition**: The complete set of usable tenpin lanes at a Bowling Center, expressed as one or more contiguous Lane Ranges. The Lane Configuration is the authoritative source for which lane pairs are available for tournament squad assignment.

**Characteristics**:

- **Replacement**: Replaced in its entirety when a physical change occurs at the center (see `ReconfigureLanes`). Never partially updated
- **USBC import default**: When seeded from USBC data, initialized as a single Lane Range spanning lane 1 to the total lane count reported by USBC (e.g., 56 lanes → `LaneRange(1, 56)`). This is an explicit import assumption — centers known to have gaps or non-standard configurations must be manually reconfigured after import
- **Odd lane count**: If USBC reports an odd total lane count, the import fails and requires manual intervention before the center record is created

**Invariants**:

- Must contain at least one Lane Range
- Lane Ranges must not overlap
- Lane Ranges must not be adjacent — if two ranges share no gap between them, they must be merged into one; any two ranges must have at least one lane of gap between them

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `LaneConfiguration` (sealed record)

---

### LaneRange

**Definition**: A contiguous block of usable tenpin lanes defined by a start lane number and an end lane number. Lanes are always used in consecutive pairs — two adjacent lanes referred to together (e.g., "lanes 25/26").

**Characteristics**:

- **PairCount**: The number of lane pairs within the range. Derived from start and end lane numbers
- **Pair enumeration**: A Lane Range can enumerate all lane pairs it contains by their actual lane numbers
- **Gap scenario**: Some centers have non-contiguous lanes due to physical changes (e.g., an arcade installed mid-center). The Lane Configuration for these centers contains multiple Lane Ranges separated by a gap. If a gap boundary falls between a natural pair, the affected lane on the usable side is also treated as out of play — the adjacent Lane Range begins at the next valid odd lane

> **Example**: A center with lanes 1–22 and 27–60 (gap at 23–26) has two Lane Ranges: `LaneRange(1, 22)` and `LaneRange(27, 60)`.

**Invariants**:

- `StartLane` must be an odd number
- `EndLane` must be an even number and at least `StartLane + 1`
- All pairs within the range are valid and usable

> **Deferred**: A `PinType` attribute (string pin vs. free fall) on Lane Range is planned for a future scope. When introduced, adjacent Lane Ranges of *different* pin types will not be merged — the adjacency merge invariant will apply only to ranges of the same type. The split center conversion scenario — where one bank of lanes converts to string pin while the other remains free fall — is the primary driver of this design.

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `LaneRange` (sealed record)

---

### ReconfigureLanes

**Definition**: A named domain operation on BowlingCenter that replaces the current Lane Configuration with a new one in its entirety. Represents a significant physical change at the center — such as lanes being removed, added, or a gap being introduced or closed. Not a routine update.

**In Code**:

- Method: `BowlingCenter.ReconfigureLanes(LaneConfiguration)`

---

## USBC Source Mapping — Bowling Centers

For reference, the following table maps USBC API fields to this domain model. Used by the import process.

| USBC Field | Disposition | Maps To |
| --- | --- | --- |
| `certnumber` | Adopted | `CertificationNumber` |
| `name` | Adopted | `Name` |
| `address`, `city`, `state`, `zip`, `country` | Adopted | `Address` |
| `phone` | Adopted (normalized on import) | `PhoneNumber` (Work) |
| `email` | Adopted | `EmailAddress` |
| `web` | Adopted | `Website` |
| `lanes` | Adopted (seeds default `LaneConfiguration`) | `LaneConfiguration` |
| `strpin` | Ignored | Deferred — lane-level pin type will be modeled on `LaneRange` in a future scope |
| `id` | Ignored | USBC internal key — not domain-relevant |
| `distance` | Ignored | Calculated at USBC query time — not a center attribute |
| `sport` | Ignored | Sport Bowling certification no longer applicable |
| `rvp`, `snackbar`, `restaurant`, `lounge`, `arcade`, `proshop`, `glow`, `childcare`, `parties`, `banquets`, `coach` | Ignored | Amenity/marketing data — no domain behavior |

---

## Maintaining This Document

This is a **living document**. As the project evolves:

**Add New Terms**:

- When introducing new business domain concepts
- When clarifying ambiguous terminology
- When adding new features with user-facing concepts

**Update Existing Terms**:

- When definitions become more precise
- When implementation details change
- When examples need clarification

**Remove Obsolete Terms**:

- When concepts are deprecated or removed
- Keep a "Deprecated Terms" section for reference

**Who Can Update**:

- Developers during implementation
- AI assistants when discovering unclear terminology
- Architects when making design decisions

**Commit Messages**:
Use format: `docs: update ubiquitous language - <term>`

Example: `docs: update ubiquitous language - add Document Refresh definition`

---

## Related Documentation

- **Architecture**: `docs/architecture/backend.md` - Technical implementation details
- **Code Standards**: `CLAUDE.md` - Coding patterns and conventions
- **PR Guidelines**: `.github/instructions/pull-request-review.instructions.md`
- **Migration Docs**: `reference/documents-implementation-overview.md` (during migration work)
