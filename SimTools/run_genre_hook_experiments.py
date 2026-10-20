"""Run an explicit JSON experiment plan; preserve flags, assembly hash and completion evidence.

Plan: {"weeks": 52, "seeds": [1001], "arms": {"unique-tag": ["--flag"]}}.
An arm may instead be {"flags": ["--flag"], "seeds": [2002], "weeks": 260}.
Common flags reproduce the handoff's F=.75 / radio spread=.1 research control.
The plan and all generated artifacts belong in SimLogs.
"""
import argparse
import hashlib
import json
import os
import re
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSEMBLY = ROOT / ".godot/mono/temp/bin/Debug/Label Man.dll"
GODOT = Path("C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe")
BASE = ["--enable-genre-market-v2", "--enable-artist-population-lifecycle", "--local-signals-read-demand",
        "--regional-arrival", "--regional-retail-access", "--regional-campaign-focus", "--economic-label-exits",
        "--chart-inertia-base=0.4", "--small-label-finance", "--calibration", "--launch-push-saturation=0.75",
        "--release-judgement-shadow", "--radio-push-spread=0.1"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("plan", type=Path)
    parser.add_argument("--parallel", type=int, choices=range(1, 5), default=4)
    args = parser.parse_args()
    plan = json.loads(args.plan.read_text(encoding="utf-8-sig"))
    if len(set(plan["seeds"])) != len(plan["seeds"]) or not 1 <= plan["weeks"] <= 522:
        parser.error("Invalid seeds or duration")
    queue = []
    for tag, spec in plan["arms"].items():
        if not re.fullmatch(r"[a-z0-9-]+", tag):
            parser.error("Invalid arm name")
        flags = spec["flags"] if isinstance(spec, dict) else spec
        seeds = spec.get("seeds", plan["seeds"]) if isinstance(spec, dict) else plan["seeds"]
        weeks = spec.get("weeks", plan["weeks"]) if isinstance(spec, dict) else plan["weeks"]
        if not 1 <= weeks <= 522 or len(set(seeds)) != len(seeds):
            parser.error(f"Invalid seeds or duration for {tag}")
        for seed in seeds:
            name = f"{tag}-{seed}-{weeks}"
            if list((ROOT / "SimLogs").glob(f"{name}-*")):
                parser.error(f"Refusing to overwrite {name}")
            queue.append((name, seed, weeks, flags))
    assembly_hash = digest(ASSEMBLY)
    manifest_path = args.plan.with_name(args.plan.stem + "-runs.json")
    if manifest_path.exists():
        parser.error("Manifest already exists; use a fresh plan")
    manifest = dict(plan=plan, assemblySha256=assembly_hash, commonFlags=BASE, results=[])
    env = dict(os.environ, DOTNET_gcServer="1", DOTNET_GCHeapCount="4")
    running = []

    def save():
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    try:
        while queue or running:
            if digest(ASSEMBLY) != assembly_hash:
                raise RuntimeError("Assembly changed during experiments")
            while queue and len(running) < args.parallel:
                name, seed, weeks, flags = queue.pop(0)
                command = [str(GODOT), "--headless", "--path", ".", "SimTools/ChartAuditRunner.tscn", "--",
                           f"--weeks={weeks}", f"--run={name}", f"--seed={seed}"] + BASE + flags
                log_path = ROOT / f"SimLogs/{name}-console.log"
                log = log_path.open("w", encoding="utf-8")
                process = subprocess.Popen(command, cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT,
                                           stdin=subprocess.DEVNULL, creationflags=subprocess.CREATE_NO_WINDOW)
                running.append((name, process, log, log_path, time.monotonic(), command, weeks))
                print(f"Started {name} (PID {process.pid})", flush=True)
            save()
            time.sleep(2)
            for job in list(running):
                name, process, log, log_path, start, command, weeks = job
                if process.poll() is None:
                    continue
                log.close()
                running.remove(job)
                content = log_path.read_text(encoding="utf-8", errors="replace")
                passed = process.returncode == 0 and f"CHART_AUDIT_COMPLETE run={name} weeks={weeks}" in content
                passed = passed and "SCRIPT ERROR" not in content and "ERROR:" not in content
                manifest["results"].append(dict(name=name, passed=passed, exitCode=process.returncode,
                                               seconds=round(time.monotonic() - start, 2), arguments=command))
                save()
                print(f"Finished {name}: passed={passed}, seconds={time.monotonic() - start:.1f}", flush=True)
                if not passed:
                    raise RuntimeError(f"Experiment failed: {log_path}")
    finally:
        # Stop only processes created by this runner if interrupted or validation fails.
        for _, process, log, _, _, _, _ in running:
            if process.poll() is None:
                subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True)
            log.close()
        save()
    if digest(ASSEMBLY) != assembly_hash:
        raise RuntimeError("Assembly changed before completion")
    print(f"All {len(manifest['results'])} experiments passed: {manifest_path}", flush=True)


if __name__ == "__main__":
    main()
