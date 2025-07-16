@echo off
where sh >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo ❌ Не найден sh. Установите Git Bash (идёт с Git for Windows).
    exit /b 1
)
sh ./run-tests.sh
exit /b %ERRORLEVEL%