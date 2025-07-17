#!/usr/bin/env bash
set -euo pipefail
PATH="/opt/homebrew/opt/util-linux/bin:$PATH"
export PATH
REPO_ROOT="$(git -C "${BASH_SOURCE%/*}" rev-parse --show-toplevel 2>/dev/null)"
# ── 1. Получаем путь к конфигу ────────────────────────────────
CFG=${1:-${CONFIG_PATH:-}}
LOG_FILE="$REPO_ROOT/.shadow/test-run-$(basename "$CFG" .json).log"
[[ -z $CFG ]] && { echo "❌ no config path"; exit 2; }

# ── 2. Detach (setsid если есть, иначе nohup) ─────────────────
if [[ -z ${SHADOW_DETACHED:-} ]]; then
  export SHADOW_DETACHED=1
  LOG="$REPO_ROOT/.shadow/test-run.log"
  {
    printf '🚀 %s cfg=%s\n' "$(date '+%F %T')" "$CFG"
    exec </dev/null
    if command -v setsid &>/dev/null; then
      exec setsid bash -c 'exec "$0" "$1"' "$0" "$CFG"
    else
      echo "⚠ setsid нет — устанавливайте util-linux" >&2
      exit 125
    fi
  } >>"$LOG" 2>&1 &
  exit 0
fi

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