---
applyTo: "**/*"
---

These are some guidelines when using the SonarQube MCP server.

# Important Tool Guidelines

## Basic usage
- **IMPORTANT**: At the start of SonarQube-focused work, disable automatic analysis with `toggle_automatic_analysis` if that tool exists in the current environment.
- **IMPORTANT**: After finishing code generation/modification, analyze all changed code files.
	- If `analyze_file_list` exists, use it for the changed files.
	- Otherwise, run `sonarqube_analyze_file` for each changed file.
	- For security-focused checks, also run `sonarqube_list_potential_security_issues` for each changed file.
- **IMPORTANT**: When SonarQube-focused work is complete, re-enable automatic analysis with `toggle_automatic_analysis` if that tool exists.
- **IMPORTANT**: Do not run SonarQube file analysis on Jupyter notebooks.

## Project Keys
- When a user mentions a project key, use `search_my_sonarqube_projects` first if that tool exists.
- If project search tooling is unavailable, ask for/confirm the exact project key instead of guessing.

## Code Language Detection
- When analyzing code snippets, try to detect the programming language from the code syntax
- If unclear, ask the user or make an educated guess based on syntax

## Branch and Pull Request Context
- Many operations support branch-specific analysis
- If user mentions working on a feature branch, include the branch parameter

## Code Issues and Violations
- After fixing issues, do not attempt to verify them via project-level issue search immediately; server-side issue indexes may lag behind file-level analysis updates.

# Common Troubleshooting

## Authentication Issues
- SonarQube requires USER tokens (not project tokens)
- When the error `SonarQube answered with Not authorized` occurs, verify the token type

## Project Not Found
- Use `search_my_sonarqube_projects` to find available projects if that tool exists
- Verify project key spelling and format

## Code Analysis Issues
- Ensure programming language is correctly specified
- Remind users that snippet analysis doesn't replace full project scans
- Provide full file content for better analysis results
