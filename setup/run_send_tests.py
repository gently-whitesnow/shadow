#!/usr/bin/env python3
"""
Run local dotnet tests and upload TRX to Shadow-Agent.

Hook-пример:
  python run_send_tests.py --config .shadow/example.configuration.json --detach
"""

from __future__ import annotations

import argparse
import getpass
import json
import logging
import os
import pathlib
import platform
import shutil
import subprocess
import sys
import uuid
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Dict, List
from urllib.parse import urlparse

#  Paths & constants
REPO_ROOT = subprocess.check_output(
    ["git", "-C", pathlib.Path(__file__).parent, "rev-parse", "--show-toplevel"],
    text=True,
    stderr=subprocess.DEVNULL,
).strip()

LOG_DIR = pathlib.Path(REPO_ROOT, ".shadow")
LOG_DIR.mkdir(parents=True, exist_ok=True)


# Config

@dataclass
class Config:
    agent_address: str
    scope: str = "default"
    test_projects: List[str] = field(default_factory=list)

    @staticmethod
    def _validate_url(u: str) -> None:
        p = urlparse(u)
        if not (p.scheme and p.netloc):
            raise ValueError(f"invalid agent_address: {u!r}")

    @classmethod
    def from_path(cls, path_: pathlib.Path) -> "Config":
        data = load_json(path_)
        try:
            agent = data["agent_address"]
        except KeyError as e:
            raise ValueError("agent_address is required in configuration") from e
        scope = data.get("scope", "default")
        test_projects = data.get("test_projects_root_absolute_path", []) or []
        cls._validate_url(agent)
        return cls(agent_address=agent, scope=scope, test_projects=test_projects)


# Helpers

def _git_cmd(*args: str) -> str:
    try:
        return subprocess.check_output(
            args, text=True, stderr=subprocess.DEVNULL, cwd=REPO_ROOT
        ).strip()
    except subprocess.CalledProcessError:
        return "unknown"


def collect_meta(scope: str, run_id: str) -> Dict[str, str]:
    """Собрать TestRunMeta → dict[str,str]"""
    return {
        "Scope": scope,
        "RunId": run_id,
        "OsUser": getpass.getuser(),
        "Branch": _git_cmd("git", "branch", "--show-current"),
        "Commit": _git_cmd("git", "rev-parse", "HEAD"),
        "MachineName": platform.node(),
        "OsPlatform": platform.platform(),
        "OsArchitecture": platform.machine(),
        "ProcessorCount": str(os.cpu_count() or 0),
        "StartUtc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        # FinishUtc добавим перед отправкой
    }


def meta_to_headers(meta: Dict[str, str]) -> Dict[str, str]:
    """Префикс Shadow- для каждого непустого поля."""
    return {f"Shadow-{k}": v for k, v in meta.items() if v}


def load_json(path_: pathlib.Path) -> Dict:
    try:
        with path_.open(encoding="utf-8") as fh:
            return json.load(fh)
    except (OSError, json.JSONDecodeError):
        return {}


# Network

def send_test_results(
    agent_address: str, trx_file: pathlib.Path, meta: Dict[str, str], log: logging.Logger
) -> None:
    url = f"{agent_address}/v1/test-results"
    headers = meta_to_headers(meta)
    log.info("📤 POST %s", url)

    try:
        import requests  # type: ignore
    except ModuleNotFoundError:
        requests = None  # noqa: N806

    try:
        if requests:
            with trx_file.open("rb") as fh:
                r = requests.post(url, data=fh, headers=headers, timeout=30)
            if r.status_code == 202:
                log.info("✅ accepted: %s", r.text.strip() or r.status_code)
            else:
                log.error("❌ %s: %s", r.status_code, r.text)
        else:
            import urllib.request

            data = trx_file.read_bytes()
            req = urllib.request.Request(url, data=data, headers=headers, method="POST")
            with urllib.request.urlopen(req, timeout=30) as resp:  # noqa: S310
                if resp.status == 202:
                    log.info("✅ accepted (urllib)")
                else:
                    log.error("❌ %s", resp.status)
    except Exception as exc:  # noqa: BLE001
        log.error("❌ send failed: %s", exc)


# Core worker

def run_worker(cfg_path: pathlib.Path, log_file: pathlib.Path) -> int:
    log = logging.getLogger("shadow")
    log.setLevel(logging.INFO)
    fh = logging.FileHandler(log_file, mode="w", encoding="utf-8")
    fh.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(message)s"))
    log.addHandler(fh)

    try:
        cfg = Config.from_path(cfg_path)
    except Exception as e:
        log.error("❌ config error: %s", e)
        return 2  # неправильный ввод/конфиг

    dotnet = shutil.which("dotnet")
    if not dotnet:
        log.error("❌ dotnet not found")
        return 127

    projects = cfg.test_projects or [""]

    run_id = str(uuid.uuid4())
    meta_common = collect_meta(cfg.scope, run_id)  # общая часть

    for proj in projects:
        trx = LOG_DIR / f"test-results-{run_id}-{uuid.uuid4().hex[:8]}.trx"
        cmd = [
            dotnet,
            "test",
            proj,
            "--no-build",
            "--verbosity",
            "normal",
            "--logger",
            f"trx;LogFileName={trx}",
        ]
        log.info("▶️ dotnet test %s", proj or "(solution)")

        exit_code = subprocess.call(
            cmd, stdin=subprocess.DEVNULL, stdout=fh.stream, stderr=fh.stream
        )
        log.info("⏹ finished code=%s", exit_code)

        if trx.exists():
            meta_common["FinishUtc"] = datetime.now(timezone.utc).isoformat(timespec="seconds")
            send_test_results(cfg.agent_address, trx, meta_common, log)
            try:
                trx.unlink()
            except OSError:
                log.warning("⚠️ cannot delete %s", trx)

        if exit_code:
            return exit_code  # пробрасываем код падения тестов

    log.info("✅ all done")
    return 0


# CLI & detach

def parse_cli() -> argparse.Namespace:
    p = argparse.ArgumentParser()
    p.add_argument("--config", dest="cfg", default=os.getenv("CONFIG_PATH"))
    det = p.add_mutually_exclusive_group()
    det.add_argument("--detach", dest="detach", action="store_true", help="background (default)")
    det.add_argument("--no-detach", dest="detach", action="store_false", help="wait for finish")
    p.set_defaults(detach=True)
    p.add_argument("--worker", action="store_true", help=argparse.SUPPRESS)
    return p.parse_args()


def spawn_detached_worker(args: argparse.Namespace, log_file: pathlib.Path) -> None:
    kw = dict(stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    if platform.system() == "Windows":
        kw["creationflags"] = subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS
    else:
        kw["start_new_session"] = True
    cmd = [sys.executable, __file__, "--worker", "--config", args.cfg, "--no-detach"]
    subprocess.Popen(cmd, **kw)  # noqa: S603,S607


def main() -> None:
    args = parse_cli()
    if not args.cfg:
        print("❌ --config не указан и $CONFIG_PATH пуст", file=sys.stderr)
        sys.exit(2)
    cfg_path = pathlib.Path(args.cfg).resolve()
    log_file = LOG_DIR / f"test-run-{cfg_path.stem}.log"

    if args.worker or not args.detach:
        sys.exit(run_worker(cfg_path, log_file))
    else:
        spawn_detached_worker(args, log_file)
        print(f"🔄 detached; log → {log_file.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    main()