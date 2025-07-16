#!/usr/bin/env sh
##############################################################################
# 0.  вычисляем абсолютный путь к репозиторию и лог-файлу
##############################################################################
REPO_ROOT=$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"

##############################################################################
# 1.  detach: первый запуск → перезапускаем сами себя в фоне и выходим
##############################################################################
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  echo "🚀 $(date '+%F %T') start (cfg=$1)" >"$LOG_FILE"
  # setsid создаёт новую сессию; nohup — игнор HUP; exec → stdout/stderr в LOG
  exec setsid nohup "$0" "$@" >>"$LOG_FILE" 2>&1 &
  exit 0             # родительский процесс завершается немедленно
fi

##############################################################################
# 2.  работа фонового экземпляра (stdout уже в LOG_FILE)
##############################################################################
CONFIG_FILE="$1"
echo "⚙️  config = $CONFIG_FILE"

# Проверяем dotnet
command -v dotnet >/dev/null 2>&1 || { echo "❌ dotnet not found"; exit 1; }

# Читаем конфиг
if [ -f "$CONFIG_FILE" ]; then
  echo "📄 read cfg OK"
  mapfile -t TESTS < <(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE")
else
  echo "⚠️ cfg not found, will test whole repo"
  TESTS=()
fi

run_tests () {
  for p in "$@"; do
    echo "🔹 test: $p"
    dotnet test "$p" --no-build --verbosity minimal
    [ $? -ne 0 ] && { echo "❌ fail: $p"; exit 1; }
  done
}

[ ${#TESTS[@]} -gt 0 ] && run_tests "${TESTS[@]}" || dotnet test --no-build --verbosity minimal
status=$?
[ $status -eq 0 ] && echo "✅ all done" || echo "❌ some tests failed ($status)"
exit $status