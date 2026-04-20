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

- Namespace: `Neba.Application.Storage` / `Neba.Domain.Storage`
- Interface: `IFileStorageService`
- Value object: `StoredFile` (domain — the storage address)
- DTO: `FileContent` (application — the retrieved content)

---

### Stored File

**Definition**: A value object that records the storage address of a file written to Azure Blob Storage. Owned by domain entities as a complex property. A Stored File does not carry the file's bytes — it is a reference that can be used to retrieve or serve the file later.

**Characteristics**:

- **Container**: The top-level logical partition in Azure Blob Storage grouping related Files (e.g., `"documents"`). Maps to the Azure Blob Storage container name. Max 63 characters
- **Path**: The blob path within the Container uniquely identifying a File (e.g., `"bylaws"`). May be flat or slash-segmented. Max 1023 characters
- **ContentType**: The MIME type of the file as stored (e.g., `"text/html"`)
- **SizeInBytes**: The size of the stored file in bytes
- **No content**: Does not carry file bytes — use `IFileStorageService.GetFileAsync()` to retrieve content

**In Code**:

- Namespace: `Neba.Domain.Storage`
- Type: `StoredFile` (sealed record, domain value object)
- EF mapping: `StoredFileConfiguration.HasStoredFile()` — maps as EF ComplexProperty on owning entities

---

### File Content

**Definition**: The materialized content of a File after it has been read from Azure Blob Storage. Returned by `IFileStorageService.GetFileAsync()`. Carries the file bytes (as a string), the MIME content type, and the metadata stored alongside the blob. Ephemeral — used within a request or handler and not persisted.

**Characteristics**:

- **Content**: The file content as a string (text files as-is; binary as Base64 or equivalent)
- **ContentType**: The MIME type (e.g., `"text/html"`)
- **Metadata**: Key-value pairs stored alongside the blob (e.g., `source_document_id`, `cached_at`)
- Distinct from `StoredFile` — `StoredFile` is the address; `FileContent` is what you receive when you fetch that address

**In Code**:

- Namespace: `Neba.Application.Storage`
- Type: `FileContent` (sealed record, application DTO)

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

- Phone numbers are stored as a collection; exactly one `Work` number is required (enforced as a domain invariant)
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

- **Identity**: Uniquely identified by its Certification Number — not by name, ownership, or address. The `CertificationNumber` property is the domain identity; there is no separate `BowlingCenterId` type. The database PK is a shadow `int` property managed by EF Core, not exposed on the domain model
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
- Domain identity: `CertificationNumber` — no `BowlingCenterId` wrapper type exists (see [ADR-0005](../adr/0005-shadow-db-pk-for-natural-key-aggregates.md))

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
- Lane Ranges with the same pin fall type must not be adjacent — adjacent ranges of the same type must be merged into one

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `LaneConfiguration` (sealed record)

---

### LaneRange

**Definition**: A contiguous block of usable tenpin lanes defined by a start lane number and an end lane number. Lanes are always used in consecutive pairs — two adjacent lanes referred to together (e.g., "lanes 25/26").

**Characteristics**:

- **PairCount**: The number of lane pairs within the range. Derived from start and end lane numbers
- **PinFallType**: Required per range. Supported values are `FreeFall` and `StringPin`
- **Pair enumeration**: A Lane Range can enumerate all lane pairs it contains by their actual lane numbers
- **Gap scenario**: Some centers have non-contiguous lanes due to physical changes (e.g., an arcade installed mid-center). The Lane Configuration for these centers contains multiple Lane Ranges separated by a gap. If a gap boundary falls between a natural pair, the affected lane on the usable side is also treated as out of play — the adjacent Lane Range begins at the next valid odd lane

> **Example**: A center with lanes 1–22 and 27–60 (gap at 23–26) has two Lane Ranges: `LaneRange(1, 22)` and `LaneRange(27, 60)`.

**Invariants**:

- `StartLane` must be an odd number
- `EndLane` must be an even number and at least `StartLane + 1`
- `PinFallType` is required
- All pairs within the range are valid and usable

**In Code**:

- Namespace: `Neba.Domain.BowlingCenters`
- Type: `LaneRange` (sealed record)

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
| `strpin` | Ignored | `PinFallType` is modeled on `LaneRange`; current import flow does not use this source field |
| `id` | Ignored | USBC internal key — not domain-relevant |
| `distance` | Ignored | Calculated at USBC query time — not a center attribute |
| `sport` | Ignored | Sport Bowling certification no longer applicable |
| `rvp`, `snackbar`, `restaurant`, `lounge`, `arcade`, `proshop`, `glow`, `childcare`, `parties`, `banquets`, `coach` | Ignored | Amenity/marketing data — no domain behavior |

---

## Bowlers

### Bowler

**Definition**: A person who exists in the NEBA system. The Bowler record is the central identity for a person — everything related to their competitive history, results, and organizational participation links back to it.

A Bowler record may represent a fully registered active participant or a historical record only (e.g., a past champion with no active registration). Only a legal name is required to create a Bowler record.

> **"Bowler" is the correct term.** Do not use *member*, *participant*, or *player* as synonyms in this system. *Member* is reserved for the separate concept of NEBA organizational membership.

**Identity**:

| Field | Type | Description |
| --- | --- | --- |
| `Id` | ULID | System-generated permanent identifier. Never changes. Used as the primary key for all API routes |
| `WebsiteId` | `int?` | Legacy ID from the old NEBA website database. Retained for data migration traceability only. Not exposed in search or UI |
| `LegacyId` | `int?` | Legacy ID from the organization management software. Retained for data migration traceability only. Not exposed in search or UI |

**Primary lookup pattern**: Search by name → select from result list → navigate by `Id`.

**In Code**:

- Namespace: `Neba.Domain.Bowlers`
- Type: `Bowler` (aggregate root)
- Identity type: `BowlerId` (ULID-backed strongly-typed ID)

---

### Name

**Definition**: A value object decomposing a Bowler's name into discrete components. Different contexts require different formats — competition results, legal documents, and formal communications each use a distinct derived format.

**Components**:

| Field | Required | Description | Examples |
| --- | --- | --- | --- |
| `FirstName` | Yes | Given name. Used in all official and display contexts | David |
| `LastName` | Yes | Family name. Used in all official and display contexts | Smith |
| `MiddleName` | No | Middle name or middle initial (e.g., `B.`). Used only in legal document contexts. Never displayed publicly | Michael, B. |
| `Suffix` | No | Generational suffix. Constrained to the `NameSuffix` enumeration | Jr. |
| `Nickname` | No | Preferred informal name. May be a derivative of the first name or entirely different. Clearable at any time | Dave, Sparky |

**Derived Name Formats**:

| Format | Formula | Used For |
| --- | --- | --- |
| **Legal Name** | `First [Middle] Last[, Suffix]` | Official documents, 1099 tax reporting, legal records |
| **Display Name** | `[Nickname \| First] Last` | Public website, tournament results, standings, awards lists |
| **Formal Name** | `First Last` | Formal communications where a nickname would be inappropriate; Hall of Fame inductee listings |

**Display Name Rules**:

- If the Bowler has a nickname set, Display Name uses the nickname in place of the first name
- If no nickname is set, Display Name falls back to first name
- Middle name never appears in Display Name or Formal Name — it is strictly for Legal Name
- Nicknames are clearable at any time

**In Code**:

- Namespace: `Neba.Domain.Bowlers`
- Type: `Name` (sealed record)
- Factory: `Name.Create(firstName, lastName, middleName?, suffix?, nickname?)`

---

### NameSuffix

**Definition**: An enumeration of valid generational name suffixes for a Bowler. Used in legal and official contexts (e.g., 1099 tax reporting).

**Valid Values**: `Jr.` `Sr.` `II` `III` `IV` `V`

Suffix is not free-text. If a value outside this set is required in the future, the enumeration is extended at that time.

**In Code**:

- Namespace: `Neba.Domain.Bowlers`
- Type: `NameSuffix` (SmartEnum with string value)

---

## Tournaments

### Tournament

**Definition**: A USBC sanctioned scratch bowling competition consisting of one or more qualifying squads followed by a single-elimination match play championship round to determine a winner. Each tournament has a Tournament Type that governs format, team size, and eligibility. Lane conditions are characterized by a Pattern Length Category and Pattern Ratio Category, which may not be known at the time of tournament creation.

**In Code**:

- Namespace: `Neba.Domain.Tournaments`
- Type: `Tournament` (aggregate root)
- Identity type: `TournamentId` (ULID-backed strongly-typed ID)

---

### Tournament Type

**Definition**: The format classification of a NEBA tournament. Determines the number of bowlers per entry (Team Size), eligibility restrictions, and match play structure. Tournament types are categorized as either active formats (currently offered by NEBA) or inactive formats (retained for historical data integrity only).

**In Code**:

- Namespace: `Neba.Domain.Tournaments`
- Type: `TournamentType` (SmartEnum)

---

### Team Size

**Definition**: The number of bowlers who compete as a single entry unit within a given Tournament Type. Singles and most specialty formats have a Team Size of 1; Doubles formats have a Team Size of 2; Trios have 3; Baker has 5.

**In Code**: `TournamentType.TeamSize`

---

### Active Format

**Definition**: A Tournament Type designation indicating whether the format is currently offered by NEBA. Inactive formats are not available for new tournament creation but are preserved in the system to maintain historical accuracy for past tournaments.

**In Code**: `TournamentType.ActiveFormat`

---

### Pattern Length Category

**Definition**: A classification of the distance (in feet) over which oil is applied to tournament lanes. Three categories are defined: **Short** (37 feet or less), **Medium** (38–42 feet), and **Long** (43 feet or more). Pattern Length Category is one of two dimensions used to characterize tournament lane conditions alongside Pattern Ratio Category. May be unknown at the time of tournament creation.

**In Code**:

- Namespace: `Neba.Domain.Tournaments`
- Type: `PatternLengthCategory` (SmartEnum)
- Property on `Tournament`: `PatternLengthCategory` (nullable)

---

### Pattern Ratio Category

**Definition**: A classification of the oil-to-dry ratio of the lane condition used in a tournament. Three categories are defined: **Sport** (ratio < 4.0), **Challenge** (4.0–8.0), and **Recreation** (≥ 8.0). Pattern Ratio Category is one of two dimensions used to characterize tournament lane conditions alongside Pattern Length Category. Sport and Challenge map directly to USBC lane condition designations of the same names; Recreation is NEBA's term for the USBC Standard/House designation. May be unknown at the time of tournament creation.

**In Code**:

- Namespace: `Neba.Domain.Tournaments`
- Type: `PatternRatioCategory` (SmartEnum)
- Property on `Tournament`: `PatternRatioCategory` (nullable)

---

### Non-Champions

**Definition**: A tournament format restricted to bowlers who have not previously won a NEBA title. Provides competitive opportunity for bowlers who have never achieved a championship win.

**In Code**: `TournamentType.NonChampions`

---

### Tournament of Champions (TOC)

**Definition**: NEBA's premier annual tournament event, restricted to past NEBA title winners. Entry eligibility is determined by whether the bowler appears on the historical NEBA champions list. The Title Sponsor's name is formally associated with this event for the duration of their sponsorship.

> **Usage**: "TOC" is the accepted abbreviation used throughout NEBA communications and in code identifiers. The full term "Tournament of Champions" is used in formal contexts.

**In Code**: `TournamentType.TournamentOfChampions`

---

### Major

**Definition**: A designation applied to NEBA's most prestigious tournament formats: the **Tournament of Champions**, the **Masters**, and the **Invitational**. The Major designation is a characteristic of the Tournament Type, not a separate entity.

---

### Left Ratio

**Definition**: The X:1 ratio of average oil volume on the inner lane boards (L18–R18) to the left outside boards (L3–L7), stored as the decimal multiplier X. A symmetric pattern has equal left and right ratios.

**In Code**: `OilPattern.LeftRatio`

---

### Right Ratio

**Definition**: The X:1 ratio of average oil volume on the inner lane boards (L18–R18) to the right outside boards (R3–R7), stored as the decimal multiplier X. A symmetric pattern has equal left and right ratios.

**In Code**: `OilPattern.RightRatio`

---

### Kegel Pattern ID

**Definition**: A GUID uniquely identifying a pattern in the Kegel public pattern library. Null when the pattern is custom-defined and has no corresponding Kegel catalog entry.

**In Code**: `OilPattern.KegelId`

---

### Custom Pattern

**Definition**: An oil pattern created outside the Kegel catalog, typically configured directly by a tournament director or house mechanic. Identified by the absence of a Kegel Pattern ID.

---

## Awards

### Season

**Definition**: The temporal boundary over which awards are calculated and assigned. A Season has an explicit start and end date rather than being derived from tournament dates, accommodating non-standard spans such as the combined 2020–2021 Season resulting from tournament cancellations.

A Season must be marked **Complete** before any awards may be assigned to bowlers. This ensures that in-progress award standings — such as a bowler currently leading Bowler of the Year — do not prematurely influence Hall of Fame point totals.

**Properties**:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | ULID | System-generated unique identifier |
| `Description` | string | Human-readable label (e.g., `"2022 Season"`, `"2020–2021 Season"`) |
| `StartDate` | date | First date of the season. Inclusive |
| `EndDate` | date | Last date of the season. Inclusive |
| `Complete` | bool | Whether the season has been closed and awards may be assigned. Defaults to `false` |

**Rules**:

- `StartDate` must be earlier than `EndDate`
- Awards may not be assigned to bowlers until `Complete` is `true`
- Once marked Complete, a Season may not be reopened
- A Tournament's dates must fall within the `StartDate` and `EndDate` of its referenced Season *(enforced at Tournament creation/update; see Tournament)*

**Relationships**:

| Related Concept | Relationship | Notes |
| --- | --- | --- |
| `Tournament` | references Season by `SeasonId` | Tournament owns the reference; Season has no knowledge of Tournaments |
| `SeasonAward` | owned by Season | Awards are assigned as children of Season after it is marked Complete |

**Domain Events**:

| Event | Trigger |
| --- | --- |
| `SeasonCompleted` | Raised when a Season is marked Complete. Downstream consumers (e.g., Hall of Fame point calculations) react to this event |

> **Out of Scope (Current)**: Hall of Fame point values associated with each award type, membership type definitions (New Member, Renewal, etc.), and the formal definition of Tournament stat-eligibility are deferred to their respective modeling sessions. No concept of season templates or recurring schedule patterns is modeled at this time.

**In Code**:

- Namespace: `Neba.Domain.Seasons`
- Type: `Season` (aggregate root)
- Identity type: `SeasonId` (ULID-backed strongly-typed ID)

---

### SeasonAward

**Definition**: A record recognizing a bowler for exceptional achievement within a Season. All season award records share a common identity structure — a system-generated `Id`, a reference to the owning `Season`, and a reference to the `Bowler` receiving the award.

There is no unique constraint per award type per season — ties produce multiple winners, each represented as a distinct record.

Awards may only be assigned after the owning Season is marked **Complete**.

**Common Properties**:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | ULID | System-generated unique identifier |
| `SeasonId` | ULID | The Season to which this award belongs |
| `BowlerId` | ULID | The bowler receiving the award |

**Rules**:

- Awards may not be assigned until the owning Season is marked `Complete`
- Once a Season is marked Complete, awards may not be corrected through the application
- Only games bowled in Stat-Eligible Tournaments contribute to award calculations
- Baker team finals games are excluded from High Average calculations

**In Code**:

- Namespace: `Neba.Domain.Seasons`
- Types: `BowlerOfTheYearAward`, `HighAverageAward`, `HighBlockAward` — three independent entity classes, each mapping to its own table. No shared base class.
- Shared identity type: `SeasonAwardId` (ULID-backed strongly-typed ID)

---

### Bowler of the Year Award

**Definition**: Recognizes overall performance across Stat-Eligible Tournaments during the Season. Awarded per **Category** — a separate award record exists for each category a bowler wins within a season.

Age eligibility for a category is evaluated as of each tournament date during the season — not as of a fixed season-level date. A bowler may win multiple categories in the same season (e.g., a bowler who is 60 years old and female may win Woman, Senior, and Super Senior).

**Properties**:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | ULID | System-generated unique identifier |
| `SeasonId` | ULID | The Season to which this award belongs |
| `BowlerId` | ULID | The bowler receiving the award |
| `Category` | `BowlerOfTheYearCategory` | The category in which the award is given |

**In Code**:

- Namespace: `Neba.Domain.Awards`
- Type: `BowlerOfTheYearAward` (entity)

---

### BowlerOfTheYearCategory

**Definition**: The competitive category under which a Bowler of the Year award is given. Age is evaluated as of each tournament date during the season.

| Value | Eligibility |
| --- | --- |
| `Open` | All eligible bowlers |
| `Woman` | Female bowlers |
| `Senior` | Age 50 or older |
| `SuperSenior` | Age 60 or older |
| `Rookie` | Bowlers paying a **New Member** membership in the current season. All subsequent seasons are renewal memberships and no longer qualify. *(See Membership for full membership type definitions — deferred)* |
| `Youth` | Bowlers under age 18 |

**In Code**:

- Namespace: `Neba.Domain.Seasons`
- Type: `BowlerOfTheYearCategory` (SmartEnum, int-valued)

---

### High Average Award

**Definition**: Recognizes the highest pinfall average per game across all Stat-Eligible Tournaments in the Season. Awarded overall — not broken out by category. If two or more bowlers share the highest average, each receives a distinct award record.

**Eligibility**: A bowler must have bowled a minimum number of games to qualify:

> **Minimum Games = floor(4.5 × number of Stat-Eligible Tournaments completed in the Season)**

Baker team finals games do not count toward a bowler's average or game total.

**Properties**:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | ULID | System-generated unique identifier |
| `SeasonId` | ULID | The Season to which this award belongs |
| `BowlerId` | ULID | The bowler receiving the award |
| `Average` | decimal | The winner's pinfall average per game |
| `TotalGames` | int? | Total games bowled across Stat-Eligible Tournaments. Nullable — not recorded for all historical seasons |
| `TournamentsParticipated` | int? | Number of Stat-Eligible Tournaments the bowler participated in. Nullable — not recorded for all historical seasons |

**In Code**:

- Namespace: `Neba.Domain.Awards`
- Type: `HighAverageAward` (entity)

---

### High Block Award

**Definition**: Recognizes the single highest 5-game pinfall total from a qualifying block (before match play) in any Stat-Eligible Tournament during the Season. Awarded overall — not broken out by category. If two or more bowlers share the highest block score, each receives a distinct award record.

**Properties**:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | ULID | System-generated unique identifier |
| `SeasonId` | ULID | The Season to which this award belongs |
| `BowlerId` | ULID | The bowler receiving the award |
| `BlockScore` | int | The winning 5-game pinfall total |

**In Code**:

- Namespace: `Neba.Domain.Awards`
- Type: `HighBlockAward` (entity)

---

## Hall of Fame

The NEBA program that formally recognizes individuals for exceptional competitive achievement, organizational service, or meaningful contribution to the organization. Inductees are honored at a Hall of Fame Banquet held every two years.

**In Code**: Namespace `Neba.Domain.HallOfFame`

---

### Banquet

**Definition**: The ceremony held every two years at which Hall of Fame inductees are formally honored. All inductees honored at a given Banquet are collectively referred to as a Class.

**Characteristics**:

- Held every two years — not annually
- All inductees across all categories are honored at the same event
- The year of the Banquet identifies the Class

---

### Class

**Definition**: The group of inductees honored at a single Banquet, identified by year (e.g., Class of 2024). A Class contains one or more Inductions.

**Characteristics**:

- Identified by the Banquet year — not a separate named entity in the system
- Grouping by year on the Hall of Fame page represents the Class

---

### Induction

**Definition**: A single record recognizing a Person under one or more Induction Categories in a given Class. If a Person is recognized under multiple categories at the same Banquet, this is recorded as one Induction — not separate records. An Induction is permanent and is never modified or revoked once recorded.

**Characteristics**:

- One record per Person per Banquet year
- May reference more than one Induction Category via bitmask
- Permanent — no modification or revocation after recording
- A Person may have Inductions across multiple years (e.g., Service in 2018, Performance in 2024)

**In Code**:

- Namespace: `Neba.Domain.HallOfFame`
- Type: `HallOfFameInduction` (aggregate root)
- Identity type: `HallOfFameId` (ULID-backed strongly-typed ID)

---

### Inductee

**Definition**: A Person who has one or more Inductions recorded in the Hall of Fame. Being an Inductee does not preclude future Induction under a different category.

---

### Induction Category

**Definition**: The basis under which a Person is inducted. There are three categories: Superior Performance, Meritorious Service, and Friend of NEBA. A single Induction may reference more than one category.

**In Code**:

- Type: `HallOfFameCategory` (SmartFlagEnum — bitmask, powers of two)
- Values: `SuperiorPerformance` (1), `MeritoriousService` (2), `FriendOfNeba` (4)

---

### Superior Performance

**Definition**: An Induction Category awarded to bowlers who have demonstrated exceptional competitive achievement in NEBA. Eligibility is determined by accumulating a minimum of 36 Hall of Fame Points through tournament titles and Bowler of the Year awards, with additional requirements around minimum years of NEBA membership and earning titles or awards across at least three different years.

**Characteristics**:

- Eligibility is calculated systematically from tracked tournament and award history
- Point accumulation logic will be implemented when the tournament and membership management migration is in scope
- Eligibility criteria and point values are published on the Hall of Fame page

---

### Hall of Fame Points

**Definition**: The numeric values assigned to tournament titles and Bowler of the Year awards used to determine eligibility for the Superior Performance Induction Category. A minimum of 36 points is required. Specific point values per award type are published on the Hall of Fame page.

---

### Meritorious Service

**Definition**: An Induction Category awarded to individuals who have dedicated significant time and contributions to NEBA in an organizational capacity, such as serving on the board, holding officer positions, or other leadership roles. Formal eligibility criteria exist and are published on the Hall of Fame page.

**Characteristics**:

- Selection process not yet documented — to be confirmed with the Hall of Fame Committee
- Not point-calculated; eligibility criteria are informational, displayed on the Hall of Fame page

---

### Friend of NEBA

**Definition**: An Induction Category awarded to individuals who have been meaningful to the organization but do not qualify under Superior Performance or Meritorious Service. Requires a formal Nomination by a NEBA member followed by a vote by the Hall of Fame Committee.

**Characteristics**:

- The broadest category — may apply to non-bowlers (e.g., center owners, sponsors, officials) who have no competitive history with NEBA
- Requires Nomination as a prerequisite; cannot be inducted without one

---

### Hall of Fame Committee

**Definition**: The body responsible for reviewing Nominations and voting on Friend of NEBA candidates.

> **Note**: The composition of the Hall of Fame Committee (who sits on it, how members are selected) is not yet documented. This is not a blocker for the current scope but will be relevant if the system ever needs to model voting or committee membership.

---

### Nomination

**Definition**: A formal action by a NEBA member to put forward a candidate for the Friend of NEBA category. A Nomination is a prerequisite for a Friend of NEBA Induction and triggers a vote by the Hall of Fame Committee.

---

### Person (Hall of Fame context)

**Definition**: Any individual who may be inducted into the Hall of Fame — including bowlers with a competitive history in NEBA and non-bowlers recognized under Friend of NEBA (e.g., center owners, sponsors, officials).

> **Dev note**: The UL uses "Person" here to reflect that not all Inductees are competitive bowlers. However, the current system models all Persons as `Bowler` records. Any individual being inducted who does not already have a Bowler record must be added to the system as one before the Induction is recorded. The induction flow does not create Bowler records. This is an acknowledged semantic gap — if a future `Person` entity is introduced to represent non-bowlers, Induction will reference it instead.

---

## Sponsors

### Sponsor

**Definition**: A company or individual that has a formal promotional relationship with NEBA in exchange for recognition and visibility across NEBA events, publications, and digital properties. Sponsors are publicly displayed on the NEBA website and referenced throughout tournament operations (e.g., live read announcements, named tournaments). `Sponsor` is the aggregate root for all sponsorship concepts.

> **"Sponsor" is the correct term.** Do not use *partner*, *advertiser*, or *supporter* as synonyms.

**Properties**:

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `Id` | ULID | Yes | System-generated unique identifier |
| `SponsorName` | string | Yes | Display name — company name (e.g., "Storm Products Inc.") or individual name (e.g., "Tony & Suzanne Reynaud") |
| `Slug` | string | Yes | URL-friendly unique identifier derived from the sponsor name; used to route to the sponsor's individual page (e.g., `/sponsors/storm`) |
| `IsCurrentSponsor` | bool | Yes | Whether the sponsor is actively associated with NEBA for the current season. Manually managed until Sponsorship Agreement tracking is implemented |
| `Priority` | int | Yes | Display order on the sponsor list. Lower values appear first. Title sponsors are assigned the highest priority (lowest number) |
| `Tier` | `SponsorTier` | Yes | Classification of sponsorship level. See SponsorTier |
| `Category` | `SponsorCategory` | Yes | Classification of the sponsor's industry or type. See SponsorCategory |
| `Logo` | `StoredFile?` | No | Storage address of the sponsor's logo in Azure Blob Storage. Null for individual sponsors who do not have a logo. Blob path convention: `sponsors/{sponsorId}/logo/{filename}` |
| `WebsiteUrl` | string? | No | External link to the sponsor's public website |
| `TagPhrase` | string? | No | Short promotional phrase provided by the sponsor for announcements and the website (e.g., "Know Your Bowling Game"). Not applicable to individual sponsors |
| `Description` | string? | No | Freeform text displayed on the sponsor's individual detail page. Written by NEBA staff or provided by the sponsor |
| `LiveReadText` | string? | No | Sponsor-provided promotional text (typically two sentences) read aloud by the Tournament Director during match play finals broadcasts. Operational use only — not publicly displayed |
| `PromotionalNotes` | string? | No | Internal-only notes capturing sponsor preferences for NEBA promotion (e.g., "QR code poster board at check-in"). Admin-visible only — never publicly displayed |
| `FacebookUrl` | string? | No | Sponsor's Facebook page URL |
| `InstagramUrl` | string? | No | Sponsor's Instagram profile URL |
| `BusinessAddress` | `Address?` | No | Sponsor's primary business address. Not applicable for individual sponsors |
| `BusinessEmail` | `EmailAddress?` | No | The sponsor's public-facing business email address (e.g., `inquiries@joesbusiness.com`). Distinct from `SponsorContact.Email`, which is the internal point-of-contact email for NEBA communications. Not applicable for individual sponsors |
| `PhoneNumbers` | `IReadOnlyCollection<PhoneNumber>` | No | Sponsor's phone numbers, keyed by type (e.g., Voice, Fax). Stored in a child table `sponsor_phone_numbers`. Not applicable for individual sponsors |
| `SponsorContact` | `ContactInfo?` | No | Designated point of contact at the sponsor's organization. For individual sponsors, may be the sponsor themselves |

**Display Rules**:

- The Sponsor List page displays all sponsors where `IsCurrentSponsor == true`, ordered by `Priority` ascending, then `SponsorName` alphabetically as a tiebreaker
- Nullable fields are not displayed on the public site when absent — this accommodates individual sponsors for whom business-oriented fields are not applicable
- `PromotionalNotes`, `SponsorContact`, `BusinessAddress`, `PhoneNumbers`, and `LiveReadText` are internal/admin only — never publicly displayed

**In Code**:

- Namespace: `Neba.Domain.Sponsors`
- Type: `Sponsor` (aggregate root)
- Identity type: `SponsorId` (ULID-backed strongly-typed ID)

---

### SponsorTier

**Definition**: A SmartEnum classifying the sponsor's level of commitment. Controls display prominence on the sponsor list page.

| Value | Name | Description |
| --- | --- | --- |
| 1 | `Title Sponsor` | The highest sponsorship level. The title sponsor's name is associated with NEBA's premier events (e.g., the TOC). Displayed at the top of the sponsor list with elevated visual prominence. Currently: Storm Products Inc. |
| 2 | `Premier` | Sponsors contributing above the standard minimum fee. Positioned between Title Sponsor and Standard in display order |
| 3 | `Standard` | The base tier corresponding to the standard annual fee. The majority of sponsors fall here |

> Future enhancement: promote to a configurable database-backed entity manageable through an admin interface.

**In Code**:

- Namespace: `Neba.Domain.Sponsors`
- Type: `SponsorTier` (SmartEnum, int-valued)

---

### SponsorCategory

**Definition**: A SmartEnum classifying the sponsor's industry or type. Used for filtering and grouping on the sponsor list.

| Value | Name | Description |
| --- | --- | --- |
| 1 | `Other` | Sponsors that do not fit into a defined category |
| 2 | `Manufacturer` | Bowling ball and equipment manufacturers (e.g., Storm, Roto Grip, 900 Global, Dexter) |
| 4 | `ProShop` | Pro shops and bowling retail (e.g., Buddies Pro Shop, Bowl Winkle's Pro Shop) |
| 8 | `BowlingCenter` | Bowling centers and lanes (e.g., Bowl-O-Rama, Old Mountain Lanes, Yankee Lanes) |
| 16 | `FinancialServices` | Financial, credit, and business services (e.g., Cambridge Credit Counseling) |
| 32 | `Technology` | Technology products and apps (e.g., Tournament Sense, Pinwheel.us) |
| 64 | `Media` | Media and streaming services (e.g., TechVision Live Streaming) |
| 128 | `Individual` | Individual or personal sponsors (e.g., Tony & Suzanne Reynaud) |

> Values are powers of two to support potential future multi-category assignment via bit flags.
> Future enhancement: promote to a configurable database-backed entity manageable through an admin interface.

**In Code**:

- Namespace: `Neba.Domain.Sponsors`
- Type: `SponsorCategory` (SmartEnum, int-valued, power-of-two)

---

### SponsorPhoneNumber

**Definition**: A phone number entry in the sponsor's `PhoneNumbers` collection. Each entry is identified by a `PhoneType` discriminator that enforces at most one number per type per sponsor at the schema level. Business phone numbers (e.g., main line, fax) are stored as a keyed collection in the `sponsor_phone_numbers` child table rather than as hardcoded columns on the sponsor record. `SponsorContact` retains a single inline `PhoneNumber` value object — a contact will only ever have one phone number.

**In Code**:

- Uses the shared `PhoneNumber` value object from `Neba.Domain.Contact`
- Composite PK `(sponsor_id, phone_type)` in `sponsor_phone_numbers` enforces the one-per-type constraint at the database level

---

### PhoneType

**Definition**: A discriminator identifying the role of a phone number on a sponsor record (e.g., Voice, Fax). Stored as a single character in the database. Enforces at most one entry per type in the `PhoneNumbers` collection via the composite primary key `(sponsor_id, phone_type)`.

**In Code**:

- Shared type from `Neba.Domain.Contact`

---

### ContactInfo

**Definition**: A value object representing the designated point of contact at a sponsor's organization for NEBA communications. Follows the same construction pattern as `Address`, `PhoneNumber`, and `EmailAddress`. Scoped to the Sponsors domain — if needed elsewhere in the future, it will be moved to `Neba.Domain.Contact` and all references updated accordingly.

**Fields**:

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `Name` | string | Yes | Full name of the contact person |
| `Phone` | `PhoneNumber` | Yes | Contact's phone number |
| `Email` | `EmailAddress` | Yes | Contact's email address |

**In Code**:

- Namespace: `Neba.Domain.Sponsors`
- Type: `ContactInfo` (sealed record, domain value object)

---

## Stats

### Bowler Season Stats

**Definition**: An aggregate capturing all performance metrics, classification flags, award points, and financial totals for a single bowler within a single Season.

**In Code**:

- Namespace: `Neba.Domain.Stats`
- Type: `BowlerSeasonStats` (aggregate root)

---

### Member

**Definition**: A bowler who holds active NEBA membership for the Season. Membership status affects award eligibility and determines whether tournament participation counts toward official statistics.

> **"Member" in this context is Season-scoped.** See also: [Bowler](#bowler) for the distinction between a Bowler record and membership status.

---

### Rookie

**Definition**: A bowler competing in their first season as a paid NEBA member. A bowler may participate as a non-member prior to their Rookie season; the classification begins only with the first paid membership.

---

### Senior

**Definition**: A bowler who is age 50 or older.

---

### Super Senior

**Definition**: A bowler who is age 60 or older. Super Senior is not mutually exclusive with Senior — a bowler who is a Super Senior satisfies both classifications simultaneously and is eligible for both award tracks.

---

### Woman

**Definition**: A bowler competing under the Women's classification, making them eligible for Woman of the Year standings.

---

### Youth

**Definition**: A bowler under the age of 18, as defined by NEBA.

---

### Eligible Tournament

**Definition**: A tournament that counts toward official season statistics and award calculations. Tournaments that are not open to the full membership (e.g., the Non-Champions event) are not eligible.

**In Code**: `BowlerSeasonStats.EligibleTournaments`

---

### Eligible Entry

**Definition**: A tournament entry that counts toward official season statistics and award calculations. A bowler may have multiple entries in a single tournament. Entries in non-eligible tournaments are excluded.

**In Code**: `BowlerSeasonStats.EligibleEntries`

---

### Cash

**Definition**: To achieve a qualifying score sufficient to earn prize money in a tournament. Each occurrence is recorded as a Cash. Cashing is distinct from advancing to the Finals.

**In Code**: `BowlerSeasonStats.Cashes`

---

### Finals (Stats context)

**Definition**: The match play round of a tournament, contested by the top qualifiers. A bowler who advances to the Finals competes in head-to-head matches.

**In Code**: `BowlerSeasonStats.Finals`

---

### Qualifying High Game

**Definition**: The highest single game a bowler bowled during the qualifying portion of any tournament in the Season. Excludes match play.

**In Code**: `BowlerSeasonStats.QualifyingHighGame`

---

### High Block

**Definition**: The highest score a bowler achieved across a 5-game qualifying block in the Season. Only tournaments with 5 or more qualifying games contribute. For tournaments with more than 5 qualifying games, each sequential 5-game group is evaluated independently.

**In Code**: `BowlerSeasonStats.HighBlock`

---

### Match Play Win

**Definition**: A head-to-head victory in the Finals round of a tournament, typically determined by higher single-game pinfall.

**In Code**: `BowlerSeasonStats.MatchPlayWins`

---

### Match Play Loss

**Definition**: A head-to-head defeat in the Finals round of a tournament.

**In Code**: `BowlerSeasonStats.MatchPlayLosses`

---

### Field Average

**Definition**: A measure of a bowler's average performance relative to the competitive field. Calculated as the bowler's qualifying average minus the field qualifying average for each tournament entered. A positive value indicates above-field performance.

**In Code**: `BowlerSeasonStats.FieldAverage`

---

### High Finish

**Definition**: The best finishing position a bowler achieved in any single tournament during the Season (e.g., 1 for first place).

**In Code**: `BowlerSeasonStats.HighFinish`

---

### Average Finish

**Definition**: The mean finishing position across all tournaments in which the bowler received a finishing position during the Season.

**In Code**: `BowlerSeasonStats.AverageFinish`

---

### Tournament Winnings

**Definition**: Total cash prize money earned across all tournaments in the Season, excluding Cup earnings.

**In Code**: `BowlerSeasonStats.TournamentWinnings`

---

### Cup

**Definition**: A competition in which points earned across a set of pre-designated tournaments are accumulated over the Season, with prize money paid to the top point earners at conclusion.

---

### Cup Earnings

**Definition**: Prize money earned specifically through Cup events during the Season.

**In Code**: `BowlerSeasonStats.CupEarnings`

---

### Credit

**Definition**: A non-cash award applied as a discount toward a future tournament entry fee. Credits may be earned by achieving a qualifying score above a defined threshold without advancing to Finals, or may be granted by the Tournament Director at their discretion.

**In Code**: `BowlerSeasonStats.Credits`

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
