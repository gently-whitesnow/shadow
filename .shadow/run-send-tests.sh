#!/usr/bin/env sh
##############################################################################
# 0.  абсолютные пути
##############################################################################
REPO_ROOT=$(git -C "$(dirname "$0")" rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"
SCRIPT_ABS="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"

##############################################################################
# 1.  DETACH (только первый проход)
##############################################################################
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  {
    echo "🚀 $(date '+%F %T') start cfg=$1  pid=$$"
    echo "cwd(parent)=$(pwd)"
    echo "PATH=$PATH"
  } >"$LOG_FILE"

  if command -v setsid >/dev/null 2>&1; then
    ( exec setsid  nohup "$SCRIPT_ABS" "$@" >>"$LOG_FILE" 2>&1 & )
  else                                     # macOS fallback
    echo "⚠ setsid not found → using nohup & disown" >>"$LOG_FILE"
    ( nohup "$SCRIPT_ABS" "$@" >>"$LOG_FILE" 2>&1 & )
  fi
  disown
  exit 0
fi

##############################################################################
# 2.  Фоновый процесс (stdout уже в LOG_FILE)
##############################################################################
echo "── child pid=$$ ppid=$PPID cwd=$(pwd) ──"

CFG="$1"
echo "cfg file = $CFG"

DOTNET=$(command -v dotnet || true)
JQ=$(command -v jq      || true)
[ -z "$DOTNET" ] && { echo "❌ dotnet not found"; exit 127; }
[ -z "$JQ"     ] && { echo "⚠ jq not found — cfg test list skipped"; }

# ── читаем список проектов
if [ -n "$JQ" ] && [ -f "$CFG" ]; then
  mapfile -t TESTS < <("$JQ" -r '.test_projects_root_absolute_path[]?' "$CFG")
  echo "projects(${#TESTS[@]}): ${TESTS[*]}"
else
  TESTS=()
fi

run() { echo "🔹 dotnet test $1"; "$DOTNET" test "$1" --no-build --verbosity normal; }

if [ ${#TESTS[@]} -gt 0 ]; then
  for P in "${TESTS[@]}"; do run "$P" || exit $?; done
else
  echo "🔹 dotnet test (solution)"; "$DOTNET" test --no-build --verbosity normal
fi
STATUS=$?
echo "── finished pid=$$ status=$STATUS ──"
exit $STATUS