---
name: dotnet-aspnet-core
version: "2.0.0"
category: "Web"
description: "Generic ASP.NET Core guidance: middleware order, configuration and options, HttpClient, async, DI lifetimes, error handling, security basics, and logging. For building an API from scratch see dotnet-aspnetcore:dotnet-webapi (reference only)."
compatibility: "Requires an ASP.NET Core project or solution."
---

# ASP.NET Core

Generic guidance for hosting, pipeline, configuration and security. Repo-specific rules live in `CLAUDE.md`, `docs/architecture/backend.md` and `docs/learnings/backend.md`; where they disagree with this file, they win. This repo's endpoints use Fast Endpoints in vertical slices; see the `vsa-ddd` skill.

## Use this skill for

- Changing hosting, middleware, auth, routing, or configuration
- Debugging request pipeline problems
- Reviewing `HttpClient`, async, DI lifetime, or error handling code

## Related skills

- Blazor UI: `dotnet-blazor` plugin skills (`author-component`, `coordinate-components`).
- Building an API from scratch: `dotnet-aspnetcore:dotnet-webapi`. **Reference only.** It recommends a service layer, which conflicts with our slices. Read it for OpenAPI and error-handling ideas.
- Docs: [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-10.0), [Best practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0).

## Workflow

1. Read `Program.cs` first: middleware order, auth scheme, endpoint registration.
2. Make the change.
3. Check with a build, tests, and a targeted request to the affected endpoint.

## Middleware order

```
ExceptionHandler → HSTS → HttpsRedirection → StaticFiles → Routing → CORS
→ Authentication → Authorization → RateLimiter → ResponseCaching → Endpoints
```

Authentication must come before authorization, and both before the endpoints. A misplaced `UseAuthorization` or `UseCors` fails without an error, so check the order whenever you add middleware.

## Configuration

- Bind settings to a class with `Configure<T>(section)` and inject `IOptions<T>` (or `IOptionsSnapshot<T>` for per-request reload).
- Keep secrets out of `appsettings.json` and source control. Use user secrets locally and a vault in deployment.
- Layer sources in this order: JSON, environment-specific JSON, environment variables, user secrets.

## HttpClient

- Get clients from `IHttpClientFactory` (or typed clients). Never `new HttpClient()` per request: it exhausts sockets.
- Do not inject scoped services into a `DelegatingHandler`. Handlers live longer than a request scope.

## Async

- Use `async`/`await` all the way. Never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in request code: it starves the thread pool.
- Never `async void` outside event handlers.
- Pass `CancellationToken` through every async call.

## Dependency injection

- Match lifetimes: a singleton must not hold a scoped service (captive dependency). Use `IServiceScopeFactory` to open a scope from a singleton.
- `DbContext` is scoped.
- Register in extension methods grouped by feature, not one long `Program.cs`.

## Error handling

- Register an `IExceptionHandler` and return `ProblemDetails`. Do not catch and swallow exceptions without logging them.
- Do not leak stack traces or internal messages to clients outside development.
- Business-rule failures are results, not exceptions (see `CLAUDE.md`: commands return `ErrorOr<T>`).

## Security

- Validate token issuer, audience, lifetime, and signing key.
- Make authorization explicit on every endpoint: policy, role, or anonymous.
- Keep HTTPS redirection and HSTS on outside development.
- Rate-limit public and expensive endpoints.
- Never log secrets, tokens, or personal data (see the `[PersonalData]` rule in `CLAUDE.md`).

## Logging

- Use `ILogger<T>` with message templates, not string interpolation. In this repo, use `[LoggerMessage]`.
- Log a caught exception before you swallow it.

## Validate

- Middleware order is intentional.
- Security and configuration changes are explicit.
- Endpoint behavior is covered by a test or smoke check.
- No blocking calls in async paths.
- No secrets in source control.
- Health checks cover production readiness.
