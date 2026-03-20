---
name: mutation-score-dotnet
description: Review surviving mutations for a .NET layer and add/update tests to kill them. Usage: /mutation-score-dotnet <Layer> <FileName>
---

The user wants to review and fix surviving mutations for a specific .NET source file.

## What to do

1. **Get the layer and file name** from the user's arguments (e.g. `Domain HallOfFameCategory`, `Domain LaneRange`). If no arguments were given, ask the user which layer and file they want to review.

2. **Run the mutation report formatter** to get the surviving mutations for that file:
   ```
   npm run mutation:ai:dotnet -- <Layer> <FileName>
   ```
   Capture the output — it lists each surviving mutation with: mutator type, line number, original code, mutant replacement, and whether it's covered or not covered by tests.

3. **Read the source file and locate the test file** so you have full context. The test project is `tests/Neba.<Layer>.Tests/`. Look for a test file whose name corresponds to the source file (e.g. `LaneRange.cs` → `LaneRangeTests.cs`).

4. **For each surviving mutation**, decide what kind of fix is needed:
   - **"not covered"** → write a new test that exercises that code path
   - **"covered, survived"** → sharpen the existing assertion to be specific enough that the mutant would fail it

5. **Follow project .NET testing rules** (from CLAUDE.md):
   - Per CLAUDE.md: display proposed test changes in the response for review rather than inserting directly (the "show, don't insert" workflow preference). Ask the user if they want you to apply them.
   - All tests need `[UnitTest]` or `[IntegrationTest]` trait and `[Component("FeatureName")]` trait
   - All Facts/Theories need `DisplayName`
   - Use `MockBehavior.Strict` for mocks; `NullLogger<T>.Instance` for loggers
   - Use test factories from `Neba.TestFactory`, never manual entity instantiation
   - Use **Shouldly** for assertions
   - Pragmatic priorities: `Block` mutations (empty method body) are highest value; `Equality`/`Logical`/`Conditional` next; deprioritize low-signal mutations
   - Arithmetic with `0` as operand: ensure test data uses non-zero values

6. **After showing fixes**, offer to re-run stryker on that layer to verify kills (if the user wants):
   ```
   cd tests/Neba.<Layer>.Tests && dotnet stryker
   ```

## Notes
- The report is read from the latest run in `tests/Neba.<Layer>.Tests/StrykerOutput/`. No need to re-run Stryker unless the user asks.
- If the report is missing, tell the user to run `cd tests/Neba.<Layer>.Tests && dotnet stryker` first.
- Keep fixes minimal — kill the mutation, don't refactor surrounding code.
- `Update` mutations (`i++`/`i--`) are excluded globally in the stryker config to prevent infinite loop hangs with the MTP runner — do not suggest adding tests for these.
