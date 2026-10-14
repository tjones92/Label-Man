"""Live-calibration A/B (SimTools/LocalSceneLiveCalibrationDirective.md).

usage: py SimTools/analyze_scene_live_calibration.py TAG [SEED ...]

1. Inertness: every CSV a run wrote, except the scene room/work diagnostics, must hash identically between the
   calibrated (live) and flat arms of the same seed.
2. Room money: per room kind and year, capacity, fill, admission, act pay; per person-year room pay and hours.
"""
import hashlib
import pathlib
import sys

import pandas as pd

LOGS = pathlib.Path(__file__).resolve().parent.parent / "SimLogs"
DIAGNOSTIC = ("-scene-room-", "-scene-work-", "-scene-room-years-")


def files(run):
    return {p.name[len(run):]: p for p in LOGS.glob(run + "-*.csv")}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def inert(tag, seed):
    live, flat = files(f"{tag}-live-{seed}"), files(f"{tag}-flat-{seed}")
    checked = differ = 0
    for suffix in sorted(set(live) | set(flat)):
        if any(d in suffix for d in DIAGNOSTIC):
            continue
        if suffix not in live or suffix not in flat:
            print(f"  missing in one arm: {suffix}")
            differ += 1
            continue
        checked += 1
        if digest(live[suffix]) != digest(flat[suffix]):
            print(f"  DIFFERS: {suffix}")
            differ += 1
    print(f"seed {seed}: {checked} economy CSVs compared, {differ} differ")
    return differ == 0 and checked > 0


def money(tag, seed, arm):
    run = f"{tag}-{arm}-{seed}"
    rooms = pd.read_csv(LOGS / f"{run}-scene-room-years-end.csv")
    rooms["fill"] = rooms.attendance / (rooms.bills * rooms.capacity)
    by_kind = rooms.groupby(["kind", "year"]).agg(
        rooms=("roomId", "nunique"), capacity=("capacity", "mean"), fill=("fill", "mean"),
        admission=("admission", "mean"), sets=("sets", "sum"), pay_per_player_night=("actPay", "sum"),
        player_nights=("playerNights", "sum"))
    by_kind["pay_per_player_night"] /= by_kind.player_nights
    print(f"\n{run}: rooms by kind and year")
    print(by_kind.drop(columns="player_nights").round(2).to_string())
    work = pd.read_csv(LOGS / f"{run}-scene-work-end.csv")
    print(f"\n{run}: per person-year")
    cols = ["stageHours", "feeShare", "attributedHours", "backgroundHours", "unbudgetedHours"]
    print(work.groupby("year")[cols].describe(percentiles=[.5, .9]).T.round(1).to_string())


def main():
    tag, seeds = sys.argv[1], sys.argv[2:] or ["1001", "2002"]
    ok = all([inert(tag, s) for s in seeds])
    print("INERT" if ok else "NOT INERT")
    for seed in seeds:
        for arm in ("flat", "live"):
            money(tag, seed, arm)


if __name__ == "__main__":
    main()
