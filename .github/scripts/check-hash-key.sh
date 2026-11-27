#!/usr/bin/env bash
# Checks that BOWLER_PII_HASH_KEY is Base64 of at least 32 bytes, as the api requires at startup.
# A bad key would otherwise fail only when the new api starts, after any migrations have run.
# Never prints the value.
set -euo pipefail

python3 -I - <<'PY'
import base64, binascii, os, sys

value = "".join(os.environ.get("BOWLER_PII_HASH_KEY", "").split())

try:
    length = len(base64.b64decode(value, validate=True))
except (binascii.Error, ValueError):
    length = 0

if length < 32:
    print("::error::BOWLER_PII_HASH_KEY must be Base64 of at least 32 bytes.", file=sys.stderr)
    sys.exit(1)

print("The bowler PII hash key is well formed.")
PY
