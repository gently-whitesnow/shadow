#!/usr/bin/env sh
set -e

echo "🧪 Запуск всех тестов в решении..."

if command -v dotnet >/dev/null 2>&1; then
  dotnet test --no-build --verbosity minimal
  STATUS=$?
else
  echo "❌ dotnet не установлен или не в PATH"
  exit 1
fi

if [ $STATUS -eq 0 ]; then
  echo "✅ Все тесты успешно завершены"
else
  echo "❌ Тесты завершились с ошибкой (код $STATUS)"
  exit $STATUS
fi