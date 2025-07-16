# Установка shadow тестирования

## Установка lefthook

```bash
brew install lefthook             # macOS
choco install lefthook            # Windows (через Chocolatey)
cargo install lefthook            # универсально
```

## Запуск тестов

```bash
./run-tests.sh # macos, linux
./run-tests.bat # windows
```

lefthook run post-commit


требуем чтобы был установлен sh

## пример конфигурации

```json
{
    "agent_address": "http://localhost:8080", // required
    "scope": "team", // required
    "test_projects_project_absolute_path": ["shadow/"], // optional (default root)
    "language": "csharp" // optional (csharp, python) (default csharp)
}
```