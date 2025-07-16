#!/usr/bin/env sh
set -e

echo "🧪 Запуск тестов..."

CONFIG_FILE="$1"
echo "⚙️ Использую конфиг: $CONFIG_FILE"

run_tests() {
  for project in "$@"; do
    echo "🔹 Тестируем: $project"
    dotnet test "$project" --no-build --verbosity minimal
    STATUS=$?
    if [ $STATUS -ne 0 ]; then
      echo "❌ Ошибка в проекте $project (код $STATUS)"
      exit $STATUS
    fi
  done
}

if ! command -v dotnet >/dev/null 2>&1; then
  echo "❌ dotnet не установлен или не в PATH"
  exit 1
fi

if [ -f "$CONFIG_FILE" ]; then
  echo "⚙️ Найден конфиг: $CONFIG_FILE"
  TEST_PATHS=$(jq -r '.test_projects_root_absolute_path[]?' "$CONFIG_FILE" 2>/dev/null || echo "")

  if [ -n "$TEST_PATHS" ]; then
    echo "📦 Используем пути из test_projects_root_absolute_path:"
    echo "$TEST_PATHS"
    run_tests $TEST_PATHS
    echo "✅ Все тесты завершены успешно (по списку)"
    exit 0
  fi
fi

echo "📂 Конфиг не найден или нет test_projects_root_absolute_path — запускаем все тесты в решении"
dotnet test --no-build --verbosity minimal
STATUS=$?

if [ $STATUS -eq 0 ]; then
  echo "✅ Все тесты успешно завершены"
else
  echo "❌ Тесты завершились с ошибкой (код $STATUS)"
  exit $STATUS
fi