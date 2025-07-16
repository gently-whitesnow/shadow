#!/usr/bin/env sh
REPO_ROOT=$(git -C "$(dirname "$0")" rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"
SCRIPT_ABS="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"

detach_child() {
  {
    echo "spawn child pid=$1"
    sleep 1
    ps -p "$1" >/dev/null && echo "child alive" || echo "child died"
  } >>"$LOG_FILE"
}

if [ -z "$DETACHED" ]; then
  export DETACHED=1
  {
    echo "🚀 $(date '+%F %T') start cfg=$1  pid=$$"
    echo "cwd(parent)=$(pwd)"
    echo "PATH=$PATH"
  } >"$LOG_FILE"

  if command -v setsid >/dev/null 2>&1; then
    setsid "$SCRIPT_ABS" "$@" >>"$LOG_FILE" 2>&1 &
    detach_child $!
  else
    echo "⚠ setsid missing → nohup" >>"$LOG_FILE"
    nohup "$SCRIPT_ABS" "$@" >>"$LOG_FILE" 2>&1 &
    detach_child $!
    disown
  fi
  exit 0
fi

echo "── child pid=$$ ppid=$PPID cwd=$(pwd) ──"

CFG="$1"
DOTNET=$(command -v dotnet || true)
JQ=$(command -v jq || true)
[ -z "$DOTNET" ] && { echo "❌ dotnet not found"; exit 127; }

[ -z "$JQ" ] && echo "⚠ jq not found — cfg list skipped"

if [ -n "$JQ" ] && [ -f "$CFG" ]; then
  mapfile -t PROJECTS < <("$JQ" -r '.test_projects_root_absolute_path[]?' "$CFG")
  echo "projects(${#PROJECTS[@]}): ${PROJECTS[*]}"
else
  PROJECTS=()
fi

run() { echo "🔹 dotnet test $1"; "$DOTNET" test "$1" --no-build --verbosity normal; }

if [ ${#PROJECTS[@]} -gt 0 ]; then
  for p in "${PROJECTS[@]}"; do run "$p" || exit $?; done
else
  echo "🔹 dotnet test (solution)"; "$DOTNET" test --no-build --verbosity normal
fi

STATUS=$?
echo "── finished $(date '+%F %T') pid=$$ status=$STATUS ──"
exit $STATUS