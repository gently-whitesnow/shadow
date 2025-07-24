#!/usr/bin/env bash
set -euo pipefail

# Настройки
RAW_BASE="https://github.com/gently-whitesnow/shadow/raw/master"

color() { printf "\033[%sm%s\033[0m\n" "$1" "$2"; }
die()   { color "31" "✖ $*"; exit 1; }
info()  { color "34" "▶ $*"; }
ok()    { color "32" "✔ $*"; }

command_exists() { command -v "$1" >/dev/null 2>&1; }

# 1. Устанавливаем Lefthook, если его нет
install_lefthook() {
  if command_exists lefthook; then
    ok "lefthook уже установлен: $(lefthook --version)"
    return
  fi

  unameOut="$(uname -s)"
  case "${unameOut}" in
    Darwin*)
      if command_exists brew; then
        info "Устанавливаю lefthook через Homebrew…"            
        brew install lefthook                                  
        return
      fi
      ;;
    Linux*)
      if command_exists apt-get; then
        info "Устанавливаю lefthook через APT репозиторий Evil Martians…"  
        curl -1sLf 'https://dl.cloudsmith.io/public/evilmartians/lefthook/setup.deb.sh' | sudo -E bash  
        sudo apt-get install -y lefthook
        return
      fi
      if command_exists snap; then
        info "Устанавливаю lefthook через snap…"                
        sudo snap install --classic lefthook                    
        return
      fi
      ;;
    MINGW*|MSYS*|CYGWIN*)
      if command_exists winget; then
        info "Устанавливаю lefthook через winget…"              
        winget install --id=evilmartians.lefthook -e --silent   
        return
      elif command_exists scoop; then
        info "Устанавливаю lefthook через Scoop…"
        scoop install lefthook                                 
        return
      fi
      ;;
  esac

  echo "Необходимо установить lefthook вручную. Пожалуйста, воспользуйтесь инструкцией https://lefthook.dev/installation/ для установки lefthook."
  exit 1
}

# 2. Клонируем / обновляем файлы проекта
fetch_file() {   # $1 — URL   $2 — локальный путь
  info "Копирую $(basename "$2")…"
  curl -fsSL "$1" -o "$2"
}

main() {
  install_lefthook

  # lefthook.yml
  if [[ ! -f lefthook.yml ]]; then
    fetch_file "${RAW_BASE}/setup/lefthook.dotnet.yml" "./lefthook.yml"
  else
    ok "lefthook.yml уже существует — пропускаю."
  fi

  # .shadow
  mkdir -p .shadow

  # run_send_tests.py (всегда обновляем)
  fetch_file "${RAW_BASE}/setup/run_send_tests.py" ".shadow/run_send_tests.py"
  chmod +x .shadow/run_send_tests.py

  # .gitignore (всегда обновляем)
  fetch_file "${RAW_BASE}/setup/.gitignore" ".shadow/.gitignore"

  # configuration.json (только если его нет)
  if [[ ! -f .shadow/configuration.json ]]; then
    fetch_file "${RAW_BASE}/setup/configuration.json" ".shadow/configuration.json"
  else
    ok "configuration.json уже существует — пропускаю."
  fi

  ok "Готово!  ✅  Теперь можно делать обычный git push — тесты запустятся автоматически."
}

main "$@"