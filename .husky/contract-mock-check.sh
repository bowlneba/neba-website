#!/bin/sh
# Nudge: flag when src/Neba.Api.Contracts changed on this branch but the e2e mock
# server wasn't touched in the same range, since a new/renamed `required` property
# on a Contracts DTO breaks Playwright silently (System.Text.Json throws on the
# missing field, and the failure looks like ordinary test flakiness).
BASE_REF="origin/main"
if ! git rev-parse --verify "$BASE_REF" >/dev/null 2>&1; then
  BASE_REF="main"
fi

if ! git rev-parse --verify "$BASE_REF" >/dev/null 2>&1; then
  exit 0
fi

MERGE_BASE="$(git merge-base "$BASE_REF" HEAD 2>/dev/null)"
if [ -z "$MERGE_BASE" ]; then
  exit 0
fi

CHANGED_FILES="$(git diff --name-only "$MERGE_BASE" HEAD)"

CONTRACT_CHANGES="$(printf '%s\n' "$CHANGED_FILES" | grep '^src/Neba\.Api\.Contracts/')"
if [ -z "$CONTRACT_CHANGES" ]; then
  exit 0
fi

if printf '%s\n' "$CHANGED_FILES" | grep -q '^tests/e2e/mock-api/mock-api-server\.ts$'; then
  exit 0
fi

echo ""
echo "Husky contract-mock-check: this branch changes Neba.Api.Contracts but not tests/e2e/mock-api/mock-api-server.ts:"
printf '%s\n' "$CONTRACT_CHANGES" | sed 's/^/  - /'
echo ""
echo "If any of these are 'required' properties on a type the Playwright mocks deserialize into"
echo "(anything under tests/e2e/mock-api/mock-api-server.ts), update the mock JSON there too —"
echo "a missing required field fails Blazor's JSON deserialization silently and looks like flaky e2e tests."
echo "Not blocking the push — skip this if the change genuinely doesn't touch a mocked response shape."
echo ""

exit 0
