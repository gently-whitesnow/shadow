#!/usr/bin/env bash
# Требуется bash ≥ 4 (Linux обычно уже содержит, macOS: brew install bash)

set -euo pipefail        # ловим любые сбои

##############################################################################
# 0. Абсолютные пути
##############################################################################
REPO_ROOT="$(git -C "${BASH_SOURCE%/*}" rev-parse --show-toplevel 2>/dev/null)"
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"
mkdir -p "${LOG_FILE%/*}"
SCRIPT_ABS="$(cd "${BASH_SOURCE%/*}" && pwd)/$(basename "$0")"

##############################################################################
# 1. Detach (первый проход)
##############################################################################
if [[ -z ${DETACHED:-} ]]; then
  export DETACHED=1
  {
    printf '🚀 %s start cfg=%s  pid=%s\n' "$(date '+%F %T')" "$1" "$$"
    printf 'cwd(parent)=%s\nPATH=%s\n' "$(pwd)" "$PATH"
  } >"$LOG_FILE"

  if command -v setsid &>/dev/null; then
    setsid "$SCRIPT_ABS" "$@" >>"$LOG_FILE" 2>&1 &
    CHILD=$!
  else
    echo "⚠ setsid missing → nohup" >>"$LOG_FILE"
    nohup "$SCRIPT_ABS" "$@" >>"$LOG_FILE" 2>&1 &
    CHILD=$!
    disown
  fi
  echo "spawn child pid=$CHILD" >>"$LOG_FILE"
  exit 0
fi

##############################################################################
# 2. Фоновый процесс
##############################################################################
echo "── child pid=$$ ppid=$PPID cwd=$(pwd) ──"

CFG="$1"
DOTNET=$(command -v dotnet || true)
[[ -z $DOTNET ]] && { echo "❌ dotnet not found"; exit 127; }

JQ=$(command -v jq || true)
[[ -z $JQ ]] && echo "⚠ jq not found — список проектов пропущен"

# Читаем список проектов (без < <(...))
PROJECTS=()
if [[ -n $JQ && -f $CFG ]]; then
  while IFS= read -r line; do
    [[ -n $line ]] && PROJECTS+=("$line")
  done < <("$JQ" -r '.test_projects_root_absolute_path[]?' "$CFG")
fi
echo "projects(${#PROJECTS[@]}): ${PROJECTS[*]:-whole-solution}"

run_test() {
  echo "🔹 dotnet test $1"
  "$DOTNET" test "$1" --no-build --verbosity normal
}

if (( ${#PROJECTS[@]} )); then
  for p in "${PROJECTS[@]}"; do run_test "$p"; done
else
  run_test # тестирует всё решение
fi

status=$?
printf '── finished %s pid=%s status=%s ──\n' "$(date '+%F %T')" "$$" "$status"
exit $status