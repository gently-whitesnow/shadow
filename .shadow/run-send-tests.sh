#!/usr/bin/env sh
##############################################################################
# 0.  абсолютный путь к репо и лог-файлу (работает и в hooks, и из любого CWD)
##############################################################################
REPO_ROOT=$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel 2>/dev/null || pwd)
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "$(dirname "$LOG_FILE")"

##############################################################################
# 1.  если setsid недоступен (macOS), fallback на nohup & disown
##############################################################################
if command -v setsid >/dev/null 2>&1; then
  DETACH_CMD="setsid"
else                                   # macOS: brew install util-linux даёт setsid
  echo "⚠️  setsid not found; using plain nohup+disown" >>"$LOG_FILE"
  DETACH_CMD=""
fi

##############################################################################
# 2.  первый запуск → перезапуск в фоне и немедленный выход
##############################################################################
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  {
    echo "🚀 $(date '+%F %T') start   cfg=$1"
    echo "  pid(parent)=$$   cwd=$(pwd)"
    echo "  PATH=$PATH"
  } >"$LOG_FILE"

  # detach: ( subshell ) & + optional setsid, stdout/err уже в ЛОГ
  (
    $DETACH_CMD nohup "$0" "$@" >>"$LOG_FILE" 2>&1
  ) &
  disown  # гарантируем не-присоединение к текущему shell
  exit 0  # → git push возвращается сразу
fi

##############################################################################
# 3.  логируем, что фоновой процесс точно стартовал
##############################################################################
echo "── detached OK  pid=$$  ppid=$PPID  cwd=$(pwd) ──"

CONFIG_FILE="$1"
echo "cfg file = $CONFIG_FILE"

##############################################################################
# 4.  проверяем dotnet и список проектов
##############################################################################
DOTNET=$(command -v dotnet || true)
[ -z "$DOTNET" ] && { echo "❌ dotnet not found"; exit 127; }
echo "dotnet = $DOTNET"

if [ -f "$CONFIG_FILE" ]; then
  mapfile -t TESTS < <(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE")
  echo "projects(${#TESTS[@]}): ${TESTS[*]}"
else
  echo "⚠️  config missing → тестируем решение целиком"
  TESTS=()
fi

##############################################################################
# 5.  сами тесты
##############################################################################
run_tests () {
  for p; do
    echo "🔹 dotnet test $p"
    $DOTNET test "$p" --no-build --verbosity normal || return $?
  done
}

if [ ${#TESTS[@]} -gt 0 ]; then
  run_tests "${TESTS[@]}"
  STATUS=$?
else
  echo "🔹 dotnet test (solution)"
  $DOTNET test --no-build --verbosity normal
  STATUS=$?
fi

echo "── finished pid=$$  status=$STATUS ──"
exit $STATUS