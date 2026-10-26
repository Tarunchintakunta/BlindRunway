#!/bin/bash
# Three-process network smoke: host (Pilot) + 2 clients (Navigator, Cabin) on 127.0.0.1.
# Usage: scripts/smoke-3clients.sh [path/to/BlindRunway.app] [port]   (build first: Blind Runway > Build > macOS (dev))
APP="${1:-Builds/macOS-dev/BlindRunway.app}"
PORT="${2:-7777}"
BIN="$APP/Contents/MacOS/Blind Runway"
LOGS="${TMPDIR:-/tmp}/blindrunway-smoke"
mkdir -p "$LOGS"
for i in 0 1 2; do "$BIN" -batchmode -nographics -smoke $i -port "$PORT" -logFile "$LOGS/seat$i.log" >/dev/null 2>&1 & pids[$i]=$!; done
fail=0
for i in 0 1 2; do wait ${pids[$i]} || fail=1; done
grep -h "\[SMOKE\]" "$LOGS"/seat*.log
exit $fail
