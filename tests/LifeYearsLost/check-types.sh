#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")" && pwd)"
log="$(mktemp)"
trap 'rm -f "$log"' EXIT
for fixture in "$root"/CompileFailures/*.fsx; do
    if dotnet fsi --exec "$fixture" >"$log" 2>&1; then
        echo "Expected a compile-time rejection: $fixture"
        exit 1
    fi
    if ! rg -q 'error FS0001:' "$log"; then
        cat "$log"
        echo "Expected a type mismatch, not an unrelated compiler failure: $fixture"
        exit 1
    fi
    echo "Type mismatch correctly rejected: $(basename "$fixture")"
done
