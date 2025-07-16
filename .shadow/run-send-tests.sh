#!/usr/bin/env sh

# запускаем в фоне
if [ -z "$DETACHED" ]; then
  export DETACHED=1
  repo_root="$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel 2>/dev/null)"
  log_file="$repo_root/.shadow/test-run.log"
  mkdir -p "$(dirname "$log_file")"
  echo "🚀 $(date '+%F %T') start" >"$log_file"
  # detach: setsid + nohup + полный редирект
  exec setsid nohup "$0" "$@" \
       >>"$log_file" 2>&1 &
  # parent выходит — git push завершается мгновенно
  exit 0
fi

# Очистка и стартовое сообщение
echo "🚀 $(date +"%Y-%m-%d %H:%M:%S") Запуск интеграционных тестов" > "$LOG_FILE"
echo "Лог: $LOG_FILE"

CONFIG_FILE="$1"

# Функция для записи в лог и на экран
log() {
  echo "$@" | tee -a "$LOG_FILE"
}

log "🧪 Запуск тестов..."
log "⚙️ Использую конфиг: $CONFIG_FILE"

run_tests() {
  for project in "$@"; do
    log "🔹 Тестируем: $project"
    dotnet test "$project" --no-build --verbosity minimal >> "$LOG_FILE" 2>&1
    STATUS=$?
    if [ $STATUS -ne 0 ]; then
      log "❌ Ошибка в проекте $project (код $STATUS)"
      exit $STATUS
    fi
  done
}

if ! command -v dotnet >/dev/null 2>&1; then
  log "❌ dotnet не установлен или не в PATH"
  exit 1
fi

if [ -f "$CONFIG_FILE" ]; then
  log "⚙️ Найден конфиг: $CONFIG_FILE"
  TEST_PATHS=$(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE" 2>/dev/null || echo "")

  if [ -n "$TEST_PATHS" ]; then
    log "📦 Используем пути из test_projects_root_absolute_path:"
    log "$TEST_PATHS"
    run_tests $TEST_PATHS
    log "✅ Все тесты завершены успешно (по списку)"
    exit 0
  fi
fi

log "📂 Конфиг не найден или нет test_projects_root_absolute_path — запускаем все тесты в решении"
dotnet test --no-build --verbosity minimal >> "$LOG_FILE" 2>&1
STATUS=$?

if [ $STATUS -eq 0 ]; then
  log "✅ Все тесты успешно завершены"
else
  log "❌ Тесты завершились с ошибкой (код $STATUS)"
  exit $STATUS
fi