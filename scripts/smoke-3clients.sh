#!/bin/bash
# Three-process network smoke: host (Pilot) + 2 clients (Navigator, Cabin) on 127.0.0.1.
# Usage: scripts/smoke-3clients.sh [path/to/BlindRunway.app]   (build first: Blind Runway > Build > macOS (dev))
APP="${1:-Builds/macOS-dev/BlindRunway.app}"
BIN="$APP/Contents/MacOS/Blind Runway"
LOGS="${TMPDIR:-/tmp}/blindrunway-smoke"
mkdir -p "$LOGS"
for i in 0 1 2; do "$BIN" -batchmode -nographics -smoke $i -logFile "$LOGS/seat$i.log" & pids[$i]=$!; done
fail=0
for i in 0 1 2; do wait ${pids[$i]} || fail=1; done
grep -h "\[SMOKE\]" "$LOGS"/seat*.log
exit $fail
