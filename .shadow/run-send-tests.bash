#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(git -C "${BASH_SOURCE%/*}" rev-parse --show-toplevel 2>/dev/null)"
LOG_FILE="$REPO_ROOT/.shadow/test-run.log"; mkdir -p "${LOG_FILE%/*}"

# ── 1. Получаем путь к конфигу ────────────────────────────────
CFG=${1:-${CONFIG_PATH:-}}
[[ -z $CFG ]] && { echo "❌ no config path"; exit 2; }

# ── 2. Detach (setsid если есть, иначе nohup) ─────────────────
# if [[ -z ${DETACHED:-} ]]; then
#   export DETACHED=1
#   echo "🚀 $(date '+%F %T') start cfg=$CFG pid=$$" >"$LOG_FILE"
#   if command -v setsid &>/dev/null; then
#     setsid "$0" "$CFG" >>"$LOG_FILE" 2>&1 &
#   else
#     echo "⚠ setsid missing → nohup" >>"$LOG_FILE"
#     nohup "$0" "$CFG" >>"$LOG_FILE" 2>&1 & disown
#   fi
#   exit 0
# fi

echo "child pid=$$ cwd=$(pwd)" >>"$LOG_FILE"

# ── 3. Проверяем утилиты ─────────────────────────────────────
DOTNET=$(command -v dotnet || true) ; [[ -z $DOTNET ]] && { echo "❌ dotnet not found" >>"$LOG_FILE"; exit 127; }
JQ=$(command -v jq || true)         ; [[ -z $JQ ]]     && { echo "⚠ jq not found"   >>"$LOG_FILE"; }

# ── 4. Формируем список проектов ─────────────────────────────
PROJECTS=()
if [[ -n $JQ && -f $CFG ]]; then
  while IFS= read -r p; do [[ $p ]] && PROJECTS+=("$p"); done < <("$JQ" -r '.test_projects_root_absolute_path[]?' "$CFG")
fi
echo "projects(${#PROJECTS[@]}): ${PROJECTS[*]-solution}" >>"$LOG_FILE"

run() { echo "🔹 dotnet test $1" >>"$LOG_FILE"; "$DOTNET" test "$1" --no-build --verbosity normal; }

if ((${#PROJECTS[@]})); then
  for p in "${PROJECTS[@]}"; do run "$p" || exit $?; done
else
  run ""                                       # тестирует всё решение
fi
echo "✅ finished $(date '+%F %T')" >>"$LOG_FILE"