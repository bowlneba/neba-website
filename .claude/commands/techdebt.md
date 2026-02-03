# Tech Debt

Scan the codebase for technical debt and architecture violations.

## Violations to Check

### Architecture Violations

1. **Cross-domain references in Domain folders**
   - Domain folders (Bowlers, Tournaments, BowlingCenters, Content) should NOT import from each other
   - Search for: imports between domain folders in `src/Neba.Domain/`

2. **Commands not returning ErrorOr<T>**
   - All commands must return `ErrorOr<T>`, never throw for business rules
   - Search for: command handlers that throw exceptions

3. **Queries returning entities instead of DTOs**
   - Query handlers should return DTOs, never domain entities
   - Search for: query handlers returning entity types

4. **Validators with business logic**
   - Validators should only handle structural validation (length, range, format)
   - No database lookups or business rules in validators

### Testing Violations

5. **Tests missing trait attributes**
   - All tests need `[UnitTest]` or `[IntegrationTest]`
   - All tests need `[Component("FeatureName")]`
   - Search for: `[Fact]` or `[Theory]` without traits

6. **Tests missing DisplayName**
   - All Facts/Theories need descriptive `DisplayName`
   - Search for: tests without `[DisplayName(...)]`

7. **Mocks not using MockBehavior.Strict**
   - All mocks should use `MockBehavior.Strict`
   - Search for: `new Mock<` without `.Strict`

8. **Manual entity instantiation in tests**
   - Use factories from `Neba.TestFactory`, never `new Entity()`
   - Search for: direct entity construction in test files

### Code Quality

9. **Unsealed classes without justification**
   - Classes should be sealed by default
   - Search for: `public class` without `sealed`

10. **Public setters on entities**
    - Domain entities should not have public setters
    - Search for: `{ get; set; }` in Domain entities

11. **Missing authorization configuration**
    - API endpoints must explicitly configure authorization
    - Search for: endpoints without `AllowAnonymous()`, `Roles()`, or `Policies()`

12. **Unbounded queries**
    - All queries should have `.Take()` limits
    - Search for: ToListAsync() without Take()

## Output Format

Report findings as:

| Category | File | Line | Issue | Severity |
|----------|------|------|-------|----------|
| Architecture | path/to/file.cs | 42 | Cross-domain import | High |
| Testing | path/to/test.cs | 15 | Missing trait | Medium |

Prioritize by severity: High (architecture), Medium (testing), Low (code quality)
