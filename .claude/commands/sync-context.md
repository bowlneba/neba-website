# Sync Context

Load essential project context for a new session.

## Steps

1. **Read architecture docs** (summarize key points):
   - `docs/architecture/backend.md` - Clean Architecture, DDD, CQRS patterns
   - `docs/architecture/blazor.md` - Component architecture, services
   - `.github/instructions/pull-request-review.instructions.md` - Code review checklist

2. **Check current state**:
   - Run `git status` to see uncommitted changes
   - Run `git log --oneline -10` to see recent commits
   - Check current branch with `git branch --show-current`

3. **Check Aspire status** (if running):
   - Use `mcp__aspire__list_resources` to see all resources
   - Identify any unhealthy or stopped resources

4. **Review active work**:
   - Check `.claude/notes/decisions.md` for recent decisions
   - Check if there are any TODO comments in recently modified files

5. **Summarize**:
   - Current branch and its purpose
   - Pending work or uncommitted changes
   - Any blockers or issues identified
