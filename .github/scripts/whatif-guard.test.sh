#!/usr/bin/env bash
# Tests whatif-guard.sh with fake `aspire` and `az` commands on the PATH. The fakes write a
# fixture main.bicep and print a fixture what-if result, so no Azure access is needed.
set -euo pipefail

script="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/whatif-guard.sh"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
failures=0

fail() { echo "FAIL: $1" >&2; failures=$((failures + 1)); }

mkdir -p "$tmp/bin"

# Fake aspire: `aspire publish ... --output-path DIR ...` writes $FIXTURE_BICEP to DIR/main.bicep.
cat > "$tmp/bin/aspire" <<'FAKE'
#!/usr/bin/env bash
out=""
while [[ $# -gt 0 ]]; do
  if [[ "$1" = "--output-path" ]]; then out="$2"; fi
  shift
done
mkdir -p "$out"
cp "$FIXTURE_BICEP" "$out/main.bicep"
FAKE

# Fake az: logs its arguments and prints $FIXTURE_WHATIF.
cat > "$tmp/bin/az" <<'FAKE'
#!/usr/bin/env bash
echo "$*" >> "$AZ_LOG"
cat "$FIXTURE_WHATIF"
FAKE
chmod +x "$tmp/bin/aspire" "$tmp/bin/az"

cat > "$tmp/main.bicep" <<'BICEP'
targetScope = 'subscription'

param resourceGroupName string

param location string

param postgresSkuName string

param minReplicas int

param withDefault string = 'kept'

@secure()
param jwtSigningKey string
BICEP

pg='/subscriptions/s/resourceGroups/rg/providers/Microsoft.DBforPostgreSQL/flexibleServers/pg'
pgdb='/subscriptions/s/resourceGroups/rg/providers/Microsoft.DBforPostgreSQL/flexibleServers/pg/databases/db'
storage='/subscriptions/s/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/st'
app='/subscriptions/s/resourceGroups/rg/providers/Microsoft.App/containerApps/web'

# Writes a what-if fixture with one change per "<type> <resource id>" argument.
whatif() {
  python3 -I - "$@" > "$tmp/whatif.json" <<'PY'
import json, sys
changes = [{"changeType": a.split(" ", 1)[0], "resourceId": a.split(" ", 1)[1], "delta": []} for a in sys.argv[1:]]
print(json.dumps({"status": "Succeeded", "changes": changes}))
PY
}

# Runs the guard in a fresh output directory. Extra KEY=VALUE arguments extend the environment.
# Sets `code` to the exit status; the summary is in $tmp/out/whatif-summary.md.
run_guard() {
  rm -rf "$tmp/out"
  : > "$tmp/az.log"
  code=0
  env PATH="$tmp/bin:$PATH" \
    FIXTURE_BICEP="$tmp/main.bicep" FIXTURE_WHATIF="$tmp/whatif.json" AZ_LOG="$tmp/az.log" \
    OUTPUT_DIR="$tmp/out" AZURE_SUBSCRIPTION_ID=sub AZURE_RESOURCE_GROUP=rg AZURE_LOCATION=eastus2 \
    Parameters__postgresSkuName=Standard_B1ms Parameters__minReplicas=1 \
    "$@" "$script" > "$tmp/stdout.log" 2>&1 || code=$?
}

# A deploy that changes only an app passes and writes a summary.
whatif "Modify $app" "NoChange $pg"
run_guard
[[ "$code" -eq 0 ]] || fail "app-only change: expected exit 0, got $code"
grep -q "Modify: Microsoft.App/containerApps/web" "$tmp/out/whatif-summary.md" || fail "app-only change: summary lacks the Modify line"

# Modifying a protected resource is allowed; only Create and Delete fail.
whatif "Modify $pg" "Modify $storage"
run_guard
[[ "$code" -eq 0 ]] || fail "protected Modify: expected exit 0, got $code"

# Deleting Postgres blocks, and the summary says why.
whatif "Delete $pg"
run_guard
[[ "$code" -eq 1 ]] || fail "Postgres delete: expected exit 1, got $code"
grep -q "Blocked" "$tmp/out/whatif-summary.md" || fail "Postgres delete: summary lacks the Blocked line"

# Deleting a database or the storage account blocks too.
whatif "Delete $pgdb"
run_guard
[[ "$code" -eq 1 ]] || fail "database delete: expected exit 1, got $code"
whatif "Delete $storage"
run_guard
[[ "$code" -eq 1 ]] || fail "storage delete: expected exit 1, got $code"

# Creating a protected resource blocks unless ALLOW_CREATE=true (a new environment).
whatif "Create $storage"
run_guard
[[ "$code" -eq 1 ]] || fail "storage create: expected exit 1, got $code"
run_guard ALLOW_CREATE=true
[[ "$code" -eq 0 ]] || fail "storage create with ALLOW_CREATE: expected exit 0, got $code"

# Creating an unprotected resource passes.
whatif "Create $app"
run_guard
[[ "$code" -eq 0 ]] || fail "app create: expected exit 0, got $code"

# Parameters: secure ones get a placeholder, ints are passed, params with a default are skipped.
args="$(cat "$tmp/az.log")"
[[ "$args" == *"jwtSigningKey=placeholder"* ]] || fail "secure param: expected a placeholder in '$args'"
[[ "$args" == *"minReplicas=1"* ]] || fail "int param: expected minReplicas=1 in '$args'"
[[ "$args" == *"resourceGroupName=rg"* ]] || fail "resource group param: expected resourceGroupName=rg in '$args'"
[[ "$args" != *"withDefault"* ]] || fail "default param: expected withDefault to be skipped in '$args'"

# A non-secure parameter with no value stops the run before what-if.
run_guard Parameters__postgresSkuName=
[[ "$code" -ne 0 ]] || fail "missing parameter: expected non-zero exit"
grep -q "postgresSkuName" "$tmp/stdout.log" || fail "missing parameter: output does not name postgresSkuName"
[[ ! -s "$tmp/az.log" ]] || fail "missing parameter: az should not run"

# A missing required setting exits non-zero.
code=0
env PATH="$tmp/bin:$PATH" -u AZURE_RESOURCE_GROUP AZURE_SUBSCRIPTION_ID=sub AZURE_LOCATION=eastus2 "$script" > /dev/null 2>&1 || code=$?
[[ "$code" -ne 0 ]] || fail "missing AZURE_RESOURCE_GROUP: expected non-zero exit"

if [[ "$failures" -gt 0 ]]; then
  echo "$failures failure(s)" >&2
  exit 1
fi
echo "whatif-guard.sh: all tests passed"
