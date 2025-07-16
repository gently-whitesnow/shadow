#!/usr/bin/env sh
##############################################################################
# 0. Абсолютные пути и переменные
##############################################################################
REPO_ROOT=$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"

# 1. Первая (родительская) копия — делаем detach и сразу выходим
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  {                                   # пишем атомарно
    echo "🚀 $(date '+%F %T') start"
    echo "  cfg   = $1"
    echo "  pid   = $$"
    echo "  cwd   = $(pwd)"
  } >"$LOG_FILE"
  # setsid → своя сессия; nohup → игнор HUP; exec → stdout в LOG_FILE
  exec setsid nohup "$0" "$@" >>"$LOG_FILE" 2>&1 &
  exit 0
fi

##############################################################################
# 2. Фоновый (рабочий) процесс
##############################################################################
echo "—— Detach OK — pid=$$ ppid=$PPID ——"
echo "cwd after setsid = $(pwd)"
echo "PATH = $PATH"

CONFIG_FILE="$1"
echo "Using CONFIG_FILE = $CONFIG_FILE"

command -v dotnet >/dev/null 2>&1 \
  && echo "dotnet found: $(command -v dotnet)" \
  || { echo "❌ dotnet not found, abort"; exit 127; }

# ────────────────────────────────────────────────────────────────────────────
# Читаем список проектов
if [ -f "$CONFIG_FILE" ]; then
  mapfile -t TESTS \
    < <(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE" 2>/dev/null)
  echo "Projects from cfg (${#TESTS[@]}): ${TESTS[*]}"
else
  echo "⚠️  cfg not found — тестируем решѐние целиком"
  TESTS=()
fi

run_tests() {
  for p in "$@"; do
    echo "🔹 dotnet test $p"
    dotnet test "$p" --no-build --verbosity normal
    s=$?
    echo "⇢ exit $s for $p"
    [ $s -ne 0 ] && return $s
  done
}

if [ ${#TESTS[@]} -gt 0 ]; then
  run_tests "${TESTS[@]}"
  STATUS=$?
else
  echo "🔹 dotnet test (solution)"
  dotnet test --no-build --verbosity normal
  STATUS=$?
fi

echo "—— finished pid=$$ status=$STATUS ——"
exit $STATUS