#!/usr/bin/env sh
###############################################################################
# 0. абсолютный путь к репо и лог-файлу
###############################################################################
REPO_ROOT=$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"

###############################################################################
# 1. Detach – только при первом вызове
###############################################################################
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  echo "🚀 $(date '+%F %T') start (cfg=$1)" >"$LOG_FILE"
  # setsid + nohup = полное отделение от TTY; exec переносит stdout и stderr
  exec setsid nohup "$0" "$@" >>"$LOG_FILE" 2>&1 &
  exit 0        # git push завершается мгновенно
fi

###############################################################################
# 2. Опциональный режим отладки
###############################################################################
[ -n "$DEBUG" ] && set -x          # выводит трассировку в лог

###############################################################################
# 3. Работа фонового экземпляра (stdout уже в LOG_FILE)
###############################################################################
CONFIG_FILE="$1"
echo "⚙️  config = $CONFIG_FILE"

command -v dotnet >/dev/null 2>&1 || { echo "❌ dotnet not found"; exit 1; }

if [ -f "$CONFIG_FILE" ]; then
  mapfile -t TESTS < <(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE")
else
  TESTS=()
fi

run_tests () {
  for p in "$@"; do
    echo "🔹 dotnet test $p"
    dotnet test "$p" --no-build --verbosity minimal
    [ $? -ne 0 ] && { echo "❌ failed: $p" ; exit 1; }
  done
}

if [ ${#TESTS[@]} -gt 0 ]; then
  run_tests "${TESTS[@]}"
else
  echo "📂 cfg empty – testing whole solution"
  dotnet test --no-build --verbosity minimal
fi

status=$?
[ $status -eq 0 ] && echo "✅ done" || echo "❌ exit $status"
exit $status