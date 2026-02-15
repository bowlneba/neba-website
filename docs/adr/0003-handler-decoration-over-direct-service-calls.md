# ADR-0003: Handler Decoration Over Direct Service Calls

## Status

Accepted

## Context

The Application layer uses `IQueryHandler<TQuery, TResponse>` and `ICommandHandler<TCommand, TResponse>` abstractions for all operations. Some handlers are thin pass-throughs that delegate directly to an infrastructure service without adding business logic.

For example, `GetDocumentQueryHandler` delegates entirely to `IDocumentsService.GetDocumentAsHtmlAsync()`. At first glance, this appears to violate YAGNI — the endpoint could inject `IDocumentsService` directly.

However, all handlers are wrapped by `TracedQueryHandlerDecorator<TQuery, TResponse>` (and the equivalent command decorator), which automatically provides:

- An `Activity` span (`query.{QueryType}`) with `ActivityKind.Server`
- Code attributes and response type tags
- Duration tracking via `IStopwatchProvider`
- Structured error logging with duration context on failure
- `ActivityStatusCode` propagation

This telemetry is invisible at the handler level but applied uniformly via the decorator pattern at registration time.

### Options Considered

1. **Always use handlers** — even for simple operations, route through the handler pipeline to get decoration benefits
2. **Skip handlers for pass-throughs** — inject the service directly in the endpoint, add telemetry manually where needed

## Decision

**Always route through handlers** (Option 1). Every query and command flows through the handler pipeline, regardless of handler complexity.

### Rationale

- **Uniform observability**: Every operation gets traced, timed, and logged without the handler author doing anything. No operations fall through the cracks.
- **Consistent endpoint pattern**: Endpoints always depend on `IQueryHandler<,>` or `ICommandHandler<,>`, never on infrastructure services directly. This keeps the API layer decoupled from infrastructure.
- **Future-proof**: When business logic is added later (caching, authorization checks, validation), the handler already exists — no rewiring needed.
- **No duplication**: Without the decorator, pass-through endpoints would need manual `ActivitySource` spans and logging to match the telemetry of handler-backed endpoints.

## Consequences

### Positive

- Every operation is traced in the Aspire dashboard and Application Insights without opt-in effort
- Code reviewers can trust that a "thin" handler still contributes telemetry value
- Adding business logic to an existing pass-through requires editing one file, not restructuring the endpoint

### Negative

- Additional class and file for operations that are purely pass-through today
- Slight indirection — a reader must know about the decorator to understand why the handler exists

### Mitigation

- This ADR documents the rationale so reviewers don't flag pass-through handlers
- The PR review guidelines reference this ADR explicitly
