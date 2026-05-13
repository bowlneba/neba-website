#!/usr/bin/env bash

set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

print_usage() {
  echo "Usage: $(basename "$0") [assembly]"
  echo
  echo "Examples:"
  echo "  ./run-all.sh"
  echo "  ./run-all.sh infrastructure"
  echo "  ./run-all.sh neba.infrastructure.tests"
  return 0
}

resolve_target_dir() {
  local input="$1"
  local normalized

  normalized="$(echo "$input" | tr '[:upper:]' '[:lower:]' | tr -d ' ._-')"

  case "$normalized" in
    api|nebaapitests)
      echo "Neba.Api.Tests"
      ;;
    application|nebaapplicationtests)
      echo "Neba.Application.Tests"
      ;;
    architecture|nebarchitecturetests|nebaarchitecturetests)
      echo "Neba.Architecture.Tests"
      ;;
    domain|nebadomaintests)
      echo "Neba.Domain.Tests"
      ;;
    infrastructure|nebainfrastructuretests)
      echo "Neba.Infrastructure.Tests"
      ;;
    website|nebawebsitetests)
      echo "Neba.Website.Tests"
      ;;
    testfactory|nebatestfactory)
      echo "Neba.TestFactory"
      ;;
    e2e)
      echo "e2e"
      ;;
    *)
      return 1
      ;;
  esac
}

passed=0
failed=0
skipped=0

target_dirs=()

if [[ $# -gt 1 ]]; then
  print_usage
  exit 2
fi

if [[ $# -eq 1 ]]; then
  if ! resolved_dir_name="$(resolve_target_dir "$1")"; then
    echo "Unknown assembly: $1"
    echo
    print_usage
    exit 2
  fi

  if [[ ! -d "$SCRIPT_DIR/$resolved_dir_name" ]]; then
    echo "Target folder does not exist: $resolved_dir_name"
    exit 2
  fi

  target_dirs+=("$SCRIPT_DIR/$resolved_dir_name/")
  echo "Running dotnet run for: $resolved_dir_name"
else
  for dir in "$SCRIPT_DIR"/*/; do
    [[ -d "$dir" ]] || continue
    target_dirs+=("$dir")
  done

  echo "Running dotnet run across test subfolders in: $SCRIPT_DIR"
fi

echo

for dir in "${target_dirs[@]}"; do
  [[ -d "$dir" ]] || continue

  dir_name="$(basename "$dir")"

  # Use the first project file in each subfolder as the run target.
  project_file=""
  while IFS= read -r -d '' file; do
    project_file="$file"
    break
  done < <(find "$dir" -maxdepth 1 -type f -name "*.csproj" -print0)

  if [[ -z "$project_file" ]]; then
    echo "SKIP  $dir_name (no .csproj found)"
    skipped=$((skipped + 1))
    continue
  fi

  echo "RUN   $dir_name"
  if (cd "$dir" && dotnet run --project "$project_file"); then
    echo "PASS  $dir_name"
    passed=$((passed + 1))
  else
    echo "FAIL  $dir_name"
    failed=$((failed + 1))
  fi

  echo
done

echo "Summary: pass=$passed fail=$failed skip=$skipped"

if [[ $failed -gt 0 ]]; then
  exit 1
fi

exit 0