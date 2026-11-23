#!/usr/bin/env bash
# Previews what a deploy would do to Azure, and fails if it would create or delete the database
# or the storage account. It generates the Bicep with `aspire publish`, then runs
# `az deployment sub what-if` on it. The what-if only reads Azure; it changes nothing.
#
# The what-if is incremental, so Azure reports Create or Modify for this deploy and never Delete.
# The Delete check stays as a guard in case a deploy ever runs in complete mode.
#
# Why: resource names come from the resource group, so a wrong group or a renamed resource would
# make the deploy create a new, empty Postgres server or storage account next to the real one.
#
# Needs: `aspire` and `az` on the PATH and an `az` login that can read the subscription.
# Environment:
#   AZURE_SUBSCRIPTION_ID  subscription to preview against
#   AZURE_RESOURCE_GROUP   resource group the deploy targets
#   AZURE_LOCATION         location of the resource group
#   Parameters__<name>     the AppHost parameters (secret ones may be missing: a placeholder is used)
#   ASPIRE_ENVIRONMENT     Aspire environment name (default: production)
#   ALLOW_CREATE           "true" to allow creating the protected resources (a new environment)
#   OUTPUT_DIR             where to write the Bicep and the report (default: whatif-output)
# Usage: whatif-guard.sh
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?AZURE_SUBSCRIPTION_ID is not set}"
: "${AZURE_RESOURCE_GROUP:?AZURE_RESOURCE_GROUP is not set}"
: "${AZURE_LOCATION:?AZURE_LOCATION is not set}"

aspire_environment="${ASPIRE_ENVIRONMENT:-production}"
output_dir="${OUTPUT_DIR:-whatif-output}"
allow_create="${ALLOW_CREATE:-false}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

mkdir -p "$output_dir"
output_dir="$(cd "$output_dir" && pwd)"
rm -rf "$output_dir/bicep"

echo "Generating Bicep..."
(cd "$repo_root/src/Neba.AppHost" && aspire publish \
  --environment "$aspire_environment" --output-path "$output_dir/bicep" --non-interactive)

main_bicep="$output_dir/bicep/main.bicep"
[[ -f "$main_bicep" ]] || { echo "::error::aspire publish did not write main.bicep" >&2; exit 1; }

# Build the --parameters list from main.bicep: each `param <name>` takes Parameters__<name>.
# A secure parameter with no value gets a placeholder; what-if never writes it to Azure.
# The list is captured first, not read through process substitution, so a missing value stops
# the script here with the Python error.
param_lines="$(python3 -I - "$main_bicep" <<'PY'
import os, re, sys

secure = False
for line in open(sys.argv[1], encoding="utf-8"):
    line = line.strip()
    if line == "@secure()":
        secure = True
        continue
    match = re.match(r"param (\w+) (string|int|bool)(\s*=.*)?$", line)
    if not match:
        continue
    name = match.group(1)
    if match.group(3):
        # Bicep uses the default unless the deploy env sets a value, as aspire deploy would.
        env_value = os.environ.get(f"Parameters__{name}", "")
        if env_value:
            print(f"{name}={env_value}")
        secure = False
        continue
    special = {
        "resourceGroupName": os.environ["AZURE_RESOURCE_GROUP"],
        "location": os.environ["AZURE_LOCATION"],
        "principalId": os.environ.get("Parameters__deployPrincipalId", ""),
    }
    value = special.get(name) or os.environ.get(f"Parameters__{name}", "")
    if not value and secure:
        value = "placeholder"
    if not value:
        sys.exit(f"Parameter '{name}' has no value: set Parameters__{name}")
    print(f"{name}={value}")
    secure = False
PY
)"

params=()
while IFS= read -r line; do
  [[ -n "$line" ]] && params+=("$line")
done <<< "$param_lines"

echo "Running what-if against $AZURE_RESOURCE_GROUP..."
az deployment sub what-if \
  --subscription "$AZURE_SUBSCRIPTION_ID" \
  --location "$AZURE_LOCATION" \
  --template-file "$main_bicep" \
  --parameters "${params[@]}" \
  --no-pretty-print \
  --result-format FullResourcePayloads \
  --output json > "$output_dir/whatif.json"

python3 -I - "$output_dir" "$allow_create" <<'PY'
import json, os, sys

output_dir, allow_create = sys.argv[1], sys.argv[2].lower() == "true"
result = json.load(open(os.path.join(output_dir, "whatif.json"), encoding="utf-8"))

# Resources whose loss or duplication would lose data.
protected = {
    "microsoft.dbforpostgresql/flexibleservers",
    "microsoft.dbforpostgresql/flexibleservers/databases",
    "microsoft.storage/storageaccounts",
}

def kind(resource_id):
    parts = resource_id.split("/providers/")[-1].split("/")
    types = [parts[0]] + parts[1::2]
    return "/".join(types).lower()

def label(resource_id):
    return resource_id.split("/providers/")[-1]

changes = result.get("changes", [])
problems = []
warnings = []
lines = []
counts = {}
for change in changes:
    change_type = change["changeType"]
    counts[change_type] = counts.get(change_type, 0) + 1
    if change_type in ("Ignore", "NoChange", "Unsupported"):
        continue
    delta = len(change.get("delta") or [])
    detail = f" ({delta} property changes)" if change_type == "Modify" else ""
    lines.append(f"- {change_type}: {label(change['resourceId'])}{detail}")
    if kind(change["resourceId"]) in protected:
        if change_type == "Delete" or (change_type == "Create" and not allow_create):
            problems.append(f"{change_type} {label(change['resourceId'])}")
        elif change_type == "Modify":
            warnings.append(label(change["resourceId"]))

report = ["## Infrastructure what-if", ""]
report.append("Counts: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
report.append("")
report += lines or ["No creates, deletes or modifications."]
report.append("")
report.append(
    "Property changes marked Modify include values Azure sets itself, so a count above zero is normal. "
    "Only a Create or Delete of Postgres or storage fails this check."
)
if warnings:
    report += ["", "**Check:** this deploy modifies " + "; ".join(warnings) + ". Read the what-if output to confirm it is intended (a SKU change, for example)."]
if result.get("status") not in (None, "Succeeded"):
    report += ["", f"**What-if did not succeed:** {result.get('status')} {result.get('error')}"]
if problems:
    report += ["", "**Blocked:** this deploy would " + "; ".join(problems) + "."]

text = "\n".join(report) + "\n"
open(os.path.join(output_dir, "whatif-summary.md"), "w", encoding="utf-8").write(text)
print(text)

summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
if summary_path:
    open(summary_path, "a", encoding="utf-8").write(text)

if result.get("status") not in (None, "Succeeded"):
    sys.exit(f"What-if did not succeed: {result.get('status')} {result.get('error')}")
if problems:
    for problem in problems:
        print(f"::error::Would {problem}", file=sys.stderr)
    sys.exit(1)
PY
