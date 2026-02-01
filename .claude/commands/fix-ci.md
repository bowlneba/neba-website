# Fix CI

Diagnose and fix CI failures.

## Step 1: Identify Failure

```bash
# List recent workflow runs
gh run list --limit 5

# Get details of failed run
gh run view <run-id> --log-failed
```

## Step 2: Diagnose by Failure Type

### Build Failures

- Check `Directory.Packages.props` for version conflicts
- Verify all projects restore correctly: `dotnet restore`
- Check for missing dependencies

### Test Failures (xUnit)

- Download test artifacts: `gh run download <run-id>`
- Check for flaky tests (look at retry counts)
- Verify test containers started (Infrastructure/API tests)
- Run locally with same filter: `dotnet test --filter "FullyQualifiedName~<test-name>"`

### Playwright E2E Failures

- Download traces: `gh run download <run-id> -n playwright-traces-chrome`
- Check browser installation
- Review mock API responses in `tests/e2e/mock-api/`
- Verify base URL configuration

### SonarCloud Failures

- Check coverage thresholds (80% required)
- Review code smells and security issues
- Check for duplicated code

## Step 3: Fix Locally

Run the same commands as CI:

```bash
# Build
dotnet build

# Unit tests
dotnet test tests/Neba.Domain.Tests
dotnet test tests/Neba.Application.Tests
dotnet test tests/Neba.Website.Tests

# Integration tests (requires Docker)
dotnet test tests/Neba.Infrastructure.Tests
dotnet test tests/Neba.Api.Tests

# E2E tests
npm run test:e2e
```

## Step 4: Verify Fix

```bash
# Check status after push
gh run watch
```

## Common Issues

| Error | Cause | Fix |
|-------|-------|-----|
| Testcontainers timeout | Docker not running | Start Docker Desktop |
| Playwright browser not found | Missing install | `npx playwright install` |
| Coverage below threshold | Uncovered code | Add missing tests |
| Package version conflict | Central versioning mismatch | Update Directory.Packages.props |
