#!/usr/bin/env bash
# Tests check-hash-key.sh with made-up keys.
set -euo pipefail

script="$(dirname "${BASH_SOURCE[0]}")/check-hash-key.sh"
failures=0

fail() { echo "FAIL: $1" >&2; failures=$((failures + 1)); }

key_of() { head -c "$1" /dev/zero | base64; }

expect_pass() {
  BOWLER_PII_HASH_KEY="$2" "$script" > /dev/null 2>&1 || fail "$1: expected success"
}

expect_failure() {
  local output
  if output="$(BOWLER_PII_HASH_KEY="$2" "$script" 2>&1)"; then
    fail "$1: expected failure"
  elif [[ -n "$2" && "$output" == *"$2"* ]]; then
    fail "$1: the output shows the value"
  fi
}

expect_pass "32 bytes" "$(key_of 32)"
expect_pass "64 bytes with a trailing newline" "$(key_of 64)"$'\n'
expect_failure "31 bytes" "$(key_of 31)"
expect_failure "not Base64" "not-a-base64-key!"
expect_failure "empty" ""

[[ "$failures" -eq 0 ]] || exit 1
echo "check-hash-key.sh: all tests passed."
