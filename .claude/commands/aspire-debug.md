# Aspire Debug

Debug Aspire resource issues using MCP tools.

## Step 1: Check Resource Status

Use `mcp__aspire__list_resources` to see all resources and their states:

- Look for resources with `state: "Unhealthy"` or `state: "Stopped"`
- Check health status for each resource
- Note any resources with errors

## Step 2: View Console Logs

For problematic resources, use `mcp__aspire__list_console_logs`:

```
mcp__aspire__list_console_logs(resourceName: "<resource-name>")
```

Common resources:

- `api` - Neba.Api service
- `web` - Neba.Website.Server
- `postgres` - PostgreSQL database
- `hangfire` - Background jobs

## Step 3: Check Structured Logs

Use `mcp__aspire__list_structured_logs` for detailed error information:

```
mcp__aspire__list_structured_logs(resourceName: "<resource-name>")
```

Look for:

- Exception details
- Error codes
- Stack traces

## Step 4: Trace Analysis

For request flow issues, use `mcp__aspire__list_traces`:

```
mcp__aspire__list_traces(resourceName: "<resource-name>")
```

Then get detailed logs for a specific trace:

```
mcp__aspire__list_trace_structured_logs(traceId: "<trace-id>")
```

## Step 5: Resource Commands

If a resource needs restart:

```
mcp__aspire__execute_resource_command(resourceName: "<name>", commandName: "resource-restart")
```

Available commands:

- `resource-start` - Start a stopped resource
- `resource-stop` - Stop a running resource
- `resource-restart` - Restart a resource

## Common Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| PostgreSQL unhealthy | Container not started | Check Docker, restart resource |
| API unhealthy | Database connection failed | Check postgres logs first |
| Website unhealthy | API not reachable | Check API health first |
| All resources stopped | Aspire crashed | Run `aspire run` again |

## Debugging Flow

```
1. List all resources
   └─> Identify unhealthy ones

2. Check console logs for unhealthy resource
   └─> Look for startup errors

3. Check structured logs
   └─> Find exception details

4. Check traces for request failures
   └─> Identify failing operations

5. Restart resource if needed
   └─> Verify it becomes healthy
```
