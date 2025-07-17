#!/usr/bin/env python3
"""
Usage:
  python run_send_tests.py --config .shadow/example.configuration.json [--detach/--no-detach]
Environment fallback:
  CONFIG_PATH   – путь к конфигу, если не передан параметр --config
"""
from __future__ import annotations
import argparse, json, os, pathlib, platform, subprocess, sys
from datetime import datetime

REPO_ROOT = subprocess.check_output(
    ["git", "-C", pathlib.Path(__file__).parent, "rev-parse", "--show-toplevel"],
    text=True, stderr=subprocess.DEVNULL).strip()

def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(add_help=False)
    p.add_argument("--config", dest="cfg", default=os.getenv("CONFIG_PATH"),
                   help="path to *.json with test_projects_root_absolute_path[]")
    det = p.add_mutually_exclusive_group()
    det.add_argument("--detach",  dest="detach", action="store_true",
                     help="run tests in background (default)")
    det.add_argument("--no-detach", dest="detach", action="store_false",
                     help="run tests synchronously")
    p.set_defaults(detach=True)
    return p.parse_args()

def build_project_list(cfg: str) -> list[str]:
    try:
        with open(cfg, encoding="utf-8") as fh:
            data = json.load(fh)
        return data.get("test_projects_root_absolute_path", [])
    except (OSError, json.JSONDecodeError):
        return []

def start_tests(cmd: list[str], logfile: pathlib.Path, detach: bool) -> int:
    log = logfile.open("ab", buffering=0)               # небуферизованный binary-append
    if detach:                                          # кросс-платформенный «nohup»
        kwargs: dict = dict(stdin=subprocess.DEVNULL,
                            stdout=log, stderr=subprocess.STDOUT)
        if platform.system() == "Windows":              # CREATE_NEW_PROCESS_GROUP|DETACHED_PROCESS
            flags = subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS
            kwargs["creationflags"] = flags
        else:                                           # POSIX: новая session через setsid
            kwargs["preexec_fn"] = os.setsid           # noqa: E731
        subprocess.Popen(cmd, **kwargs)                 # сразу вернёмся в hook
        return 0
    else:                                               # синхронный режим
        return subprocess.call(cmd, stdin=subprocess.DEVNULL,
                               stdout=log, stderr=subprocess.STDOUT)

def main() -> None:
    opts = parse_args()
    if not opts.cfg:
        sys.exit("❌ --config не указан и $CONFIG_PATH пуст")

    cfg_path = pathlib.Path(opts.cfg)
    stem = cfg_path.stem
    log_file = pathlib.Path(REPO_ROOT, ".shadow", f"test-run-{stem}.log")
    log_file.parent.mkdir(parents=True, exist_ok=True)

    # Очищаем лог-файл при каждом запуске
    with log_file.open("w", encoding="utf-8") as log:
        log.write(f"🚀 {datetime.now():%F %T} cfg={cfg_path} argv={sys.argv}\n")

    dotnet = shutil.which("dotnet")
    if not dotnet:
        with log_file.open("a", encoding="utf-8") as log:
            log.write("❌ dotnet not found in PATH\n")
        sys.exit(127)

    projects = build_project_list(str(cfg_path))
    if not projects:                                   # тестируем всё решение
        projects = [""]

    for proj in projects:
        cmd = [dotnet, "test", proj, "--no-build", "--verbosity", "normal"]
        exit_code = start_tests(cmd, log_file, opts.detach)
        if not opts.detach and exit_code:
            sys.exit(exit_code)

    if not opts.detach:
        with log_file.open("a", encoding="utf-8") as log:
            log.write(f"✅ finished {datetime.now():%F %T}\n")

if __name__ == "__main__":
    import shutil
    main()