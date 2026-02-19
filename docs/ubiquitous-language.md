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
