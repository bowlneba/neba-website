# Use-case folder and endpoint checklist

Read this when you shape a new use-case folder. The `new-endpoint` skill gives the step order.

## Use-case folder

```
Features/{Feature}/
├── {Feature}EndpointGroup.cs
├── {Feature}Errors.cs
├── Domain/                      aggregates, entities, value objects, typed IDs
└── {UseCase}/
    ├── {UseCase}Endpoint.cs
    ├── {UseCase}Summary.cs
    ├── {UseCase}Command.cs or Query.cs
    ├── {UseCase}CommandHandler.cs or QueryHandler.cs
    └── {UseCase}RequestValidator.cs   (when the request has rules)
```

Request, Input and Response records live in `Neba.Api.Contracts/{Feature}/{UseCase}/`. The Refit interface is there too. Public contract types carry XML `<summary>` and `<example>` docs.

Endpoint, handler and domain types are `internal`. Endpoints are `sealed`.

### Endpoint checklist

- [ ] Folder holds Endpoint, Summary and (if needed) Validator
- [ ] Request wraps Input for commands
- [ ] Authorization is explicit: `AllowAnonymous()`, `Roles()` or `Policies()`
- [ ] Group, API version via `Options(...)` (not in the path), tags
- [ ] `WithName()` in `Description` for OpenAPI and `CreatedAt` links
- [ ] `Produces()` and `ProducesProblemDetails()` for every status code
- [ ] Handler errors flow through `AddError()` and `Send.ErrorsAsync(status)` (see `SponsorMutationResultSender`)
- [ ] Validator covers structure only (400, automatic)
- [ ] Summary has request and response examples
- [ ] Contracts, Refit interface and the e2e mock updated
- [ ] Tests per `docs/learnings/testing.md`

The `new-endpoint` skill gives the step order.
