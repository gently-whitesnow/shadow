#!/usr/bin/env sh
##############################################################################
# 0. Resolve absolute repo root + log file
##############################################################################
REPO_ROOT=$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"

##############################################################################
# 1. First pass → detach and exit
##############################################################################
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  {
    echo "🚀 $(date '+%F %T') start cfg=$1  pid=$$"
    echo "cwd (parent) = $(pwd)"
    echo "PATH = $PATH"
  } >"$LOG_FILE"

  # macOS ships without /usr/bin/setsid ➜ fall back to plain nohup
  DETACH=$(command -v setsid 2>/dev/null || echo "")
  (
    $DETACH nohup "$0" "$@" >>"$LOG_FILE" 2>&1
  ) &
  disown
  exit 0                      # git push returns now
fi

##############################################################################
# 2. Child: runtime probe
##############################################################################
echo "── child  pid=$$  ppid=$PPID  cwd=$(pwd) ──"
echo "which dotnet → $(command -v dotnet || echo MISSING)"
echo "which jq     → $(command -v jq || echo MISSING)"

##############################################################################
# 3. Config + test list
##############################################################################
CONFIG_FILE="$1"
if [ -f "$CONFIG_FILE" ]; then
  echo "config found"
  mapfile -t PROJECTS < <(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE")
  echo "projects(${#PROJECTS[@]}): ${PROJECTS[*]}"
else
  echo "cfg NOT found → full solution test"
  PROJECTS=()
fi

##############################################################################
# 4. Run tests
##############################################################################
run() { dotnet test "$1" --no-build --verbosity normal; }

if [ ${#PROJECTS[@]} -gt 0 ]; then
  for p in "${PROJECTS[@]}"; do
    echo "🔹 $p"
    run "$p" || { echo "❌ fail ↩︎ $p"; exit 1; }
  done
else
  echo "🔹 dotnet test (solution)"
  run || exit $?
fi

echo "✅ finished pid=$$"