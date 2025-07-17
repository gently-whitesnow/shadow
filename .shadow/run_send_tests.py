#!/usr/bin/env python3
"""
Run local dotnet tests and, optionally, upload TRX to Shadow-Agent.

Usage (in Git hook):
    python run_send_tests.py --config .shadow/example.configuration.json --detach
    python run_send_tests.py --config .shadow/example.configuration.json --no-detach
"""

from __future__ import annotations
import argparse
import json
import logging
import os
import pathlib
import platform
import subprocess
import sys
import uuid
from datetime import datetime
from typing import List, Dict, Optional

# ---------------------------------------------------------------------------#
#  Configuration & helpers
# ---------------------------------------------------------------------------#

REPO_ROOT = subprocess.check_output(
    ["git", "-C", pathlib.Path(__file__).parent, "rev-parse", "--show-toplevel"],
    text=True, stderr=subprocess.DEVNULL
).strip()

LOG_DIR = pathlib.Path(REPO_ROOT, ".shadow")
LOG_DIR.mkdir(exist_ok=True, parents=True)


def build_project_list(cfg: pathlib.Path) -> List[str]:
    try:
        with cfg.open(encoding="utf-8") as fh:
            data = json.load(fh)
        return data.get("test_projects_root_absolute_path", [])
    except (OSError, json.JSONDecodeError):
        return []


def load_json(cfg: pathlib.Path) -> Dict:
    try:
        with cfg.open(encoding="utf-8") as fh:
            return json.load(fh)
    except (OSError, json.JSONDecodeError):
        return {}


def send_test_results(
    agent_address: str,
    trx_file: pathlib.Path,
    run_id: str,
    scope: str,
    git_meta: Dict,
    log: logging.Logger,
) -> None:
    """Отправка .trx одним HTTP-запросом без ретраев.  requests → urllib fallback."""
    url = f"{agent_address}/v1/test-results"
    hdr = {
        "X-Shadow-RunId": run_id,
        "X-Shadow-Project": scope,
        "X-Shadow-Branch": git_meta["branch"],
        "X-Shadow-Commit": git_meta["commit"],
    }
    log.info("📤 sending results to %s", url)

    try:
        try:
            import requests  # noqa: WPS433
        except ModuleNotFoundError:
            raise ImportError

        with trx_file.open("rb") as fh:
            resp = requests.post(url, data=fh, headers=hdr, timeout=30)
        if resp.status_code == 202:
            log.info("✅ server accepted runId=%s", resp.json().get("runId", "unknown"))
        else:
            log.error("❌ server responded %s: %s", resp.status_code, resp.text)
    except ImportError:
        import urllib.request
        req = urllib.request.Request(url, data=trx_file.read_bytes(), headers=hdr, method="POST")
        with urllib.request.urlopen(req, timeout=30) as resp:  # noqa: S310
            if resp.status == 202:
                log.info("✅ server accepted response")
            else:
                log.error("❌ server responded %s", resp.status)
    except Exception as exc:  # noqa: WPS112
        log.error("❌ sending failed: %s", exc)


def git_meta() -> Dict[str, str]:
    def _cmd(*args: str) -> str:
        try:
            return subprocess.check_output(args, text=True, stderr=subprocess.DEVNULL, cwd=REPO_ROOT).strip()
        except subprocess.CalledProcessError:
            return "unknown"

    branch = _cmd("git", "branch", "--show-current")
    commit = _cmd("git", "rev-parse", "HEAD")
    try:
        remote = _cmd("git", "remote", "get-url", "origin")
        project = remote.split("/")[-1].removesuffix(".git")
    except Exception:
        project = pathlib.Path(REPO_ROOT).name
    return {"branch": branch, "commit": commit, "project": project}


# ---------------------------------------------------------------------------#
#  Worker
# ---------------------------------------------------------------------------#


def run_worker(cfg_path: pathlib.Path, log_file: pathlib.Path) -> int:
    """Синхронная работа: запустить dotnet, отправить отчёт, удалить .trx."""
    log = logging.getLogger("shadow")
    log.setLevel(logging.INFO)
    fh = logging.FileHandler(log_file, mode='w', encoding="utf-8")
    fh.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(message)s"))
    log.addHandler(fh)

    meta = git_meta()
    log.info("🚀 start, project=%s branch=%s commit=%s", meta["project"], meta["branch"], meta["commit"][:8])

    cfg = load_json(cfg_path)
    agent = cfg.get("agent_address")
    scope = cfg.get("scope", "default")

    dotnet = shutil.which("dotnet")
    if not dotnet:
        log.error("❌ dotnet not found in PATH")
        return 127

    projects = build_project_list(cfg_path) or [""]

    run_id = str(uuid.uuid4())
    for prj in projects:
        trx = LOG_DIR / f"test-results-{run_id}-{uuid.uuid4().hex[:8]}.trx"
        cmd = [dotnet, "test", prj, "--no-build", "--verbosity", "normal", "--logger", f"trx;LogFileName={trx}"]
        log.info("▶️ dotnet test %s", prj or "(solution)")

        exit_code = subprocess.call(cmd, stdin=subprocess.DEVNULL, stdout=fh.stream, stderr=fh.stream)
        log.info("⏹ dotnet finished with code %s", exit_code)

        if trx.exists() and agent:
            log.info("📊 report %s (%s bytes)", trx.name, trx.stat().st_size)
            send_test_results(agent, trx, run_id, scope, meta, log)
            try:
                trx.unlink()  # удалить после попытки отправки
            except OSError:
                log.warning("⚠️ cannot delete %s", trx)
        elif not trx.exists():
            log.warning("⚠️ TRX not found for %s", prj or "(solution)")
        elif not agent:
            log.info("ℹ️ agent_address not configured, skip upload")

        if exit_code:
            return exit_code

    log.info("✅ all done")
    return 0


# ---------------------------------------------------------------------------#
#  CLI & detach wrapper
# ---------------------------------------------------------------------------#

def parse_cli() -> argparse.Namespace:
    p = argparse.ArgumentParser()
    p.add_argument("--config", dest="cfg", default=os.getenv("CONFIG_PATH"))
    det = p.add_mutually_exclusive_group()
    det.add_argument("--detach", dest="detach", action="store_true", help="run in background (default)")
    det.add_argument("--no-detach", dest="detach", action="store_false", help="run synchronously")
    p.set_defaults(detach=True)
    p.add_argument("--worker", action="store_true", help=argparse.SUPPRESS)  # internal use
    return p.parse_args()


def spawn_detached_worker(args: argparse.Namespace, log_file: pathlib.Path) -> None:
    kwargs: Dict = dict(stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    if platform.system() == "Windows":
        kwargs["creationflags"] = subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS
    else:
        kwargs["start_new_session"] = True  # Python ≥3.9
    cmd = [sys.executable, __file__, "--worker", "--config", args.cfg, "--no-detach"]
    subprocess.Popen(cmd, **kwargs)  # noqa: S603,S607


def main() -> None:
    args = parse_cli()
    if not args.cfg:
        sys.exit("❌ --config не указан и $CONFIG_PATH пуст")
    cfg_path = pathlib.Path(args.cfg).resolve()
    log_file = LOG_DIR / f"test-run-{cfg_path.stem}.log"

    if args.worker or not args.detach:
        # Синхронный режим (или воркер после detach)
        rc = run_worker(cfg_path, log_file)
        sys.exit(rc)
    else:
        # Detach wrapper
        spawn_detached_worker(args, log_file)
        print(f"🔄 tests detached; see log {log_file.relative_to(REPO_ROOT)}")
        sys.exit(0)


if __name__ == "__main__":
    import shutil  # импорт после __main__ для читабельности
    main()