#!/usr/bin/env sh
set -e

echo "📦 Установка прав на исполняемые файлы..."

chmod +x ./.shadow/run-tests.sh

echo "✅ Инициализация завершена"

brew install util-linux
# echo 'export PATH="/opt/homebrew/opt/util-linux/bin:$PATH"' >> ~/.zshrc
# source ~/.zshrc
