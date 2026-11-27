#!/usr/bin/env bash
# Tests check-hash-key.sh with made-up keys.
set -euo pipefail

script="$(dirname "${BASH_SOURCE[0]}")/check-hash-key.sh"
failures=0

fail() {
  local message="$1"
  echo "FAIL: $message" >&2
  failures=$((failures + 1))
}

key_of() {
  local length="$1"
  head -c "$length" /dev/zero | base64
}

expect_pass() {
  local name="$1" key="$2"
  BOWLER_PII_HASH_KEY="$key" "$script" > /dev/null 2>&1 || fail "$name: expected success"
}

expect_failure() {
  local name="$1" key="$2" output
  if output="$(BOWLER_PII_HASH_KEY="$key" "$script" 2>&1)"; then
    fail "$name: expected failure"
  elif [[ -n "$key" && "$output" == *"$key"* ]]; then
    fail "$name: the output shows the value"
  fi
}

expect_pass "32 bytes" "$(key_of 32)"
expect_pass "64 bytes with a trailing newline" "$(key_of 64)"$'\n'
expect_failure "31 bytes" "$(key_of 31)"
expect_failure "not Base64" "not-a-base64-key!"
expect_failure "empty" ""

[[ "$failures" -eq 0 ]] || exit 1
echo "check-hash-key.sh: all tests passed."
