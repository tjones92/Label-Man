"""Measure F0 from minute-level OpeningCashLockRunner reports and replay the proposed fatigue rules.

Usage: python SimTools/analyze_fatigue_baseline.py "SimLogs/fatigue-f0-*-opening.json"
"""

import argparse
import json
from collections import Counter
from dataclasses import dataclass
from glob import glob
from pathlib import Path


HOUR = 60
REGULAR_END = 18 * HOUR
OVERTIME_END = 21 * HOUR


@dataclass(frozen=True)
class Day:
    start: int
    end: int
    long_bus_legs: int

    @property
    def overtime(self):
        return max(0, min(self.end, OVERTIME_END) - max(self.start, REGULAR_END))

    @property
    def night(self):
        return max(0, self.end - max(self.start, OVERTIME_END))


def read_run(path):
    report = json.loads(Path(path).read_text(encoding="utf-8"))
    if report["status"] != "done" or not report["daily"]:
        raise ValueError(f"{path}: incomplete run")

    days = []
    for index, row in enumerate(report["daily"]):
        if row["day"] != index or not {"startMinute", "endMinute", "longBusLegs"} <= row.keys():
            raise ValueError(f"{path}: expected consecutive daily rows with clock readings")
        day = Day(row["startMinute"], row["endMinute"], row["longBusLegs"])
        if not 0 <= day.start <= day.end or day.long_bus_legs < 0:
            raise ValueError(f"{path}: invalid time or bus count on day {index}")
        days.append(day)

    profile = {"discerning": "busy", "frugal": "plain"}[report["policy"]]
    key = (report["seed"], report["archetype"], report["town"], profile)
    return key, days


def wake_states(days, early_recovery=5, overtime_recovery=2, quiet_bonus=2):
    # Keep partial hours in owed minutes so splitting an action cannot change the cost.
    owed = 4 * HOUR
    states = Counter()
    for day in days:
        if owed >= 6 * HOUR:
            states["SPENT"] += 1
        elif owed >= 3 * HOUR:
            states["WORN"] += 1
        else:
            states["FRESH"] += 1

        owed = min(12 * HOUR, owed + day.overtime + 2 * day.night + day.long_bus_legs * HOUR)
        recovery = 0
        if day.end <= REGULAR_END:
            recovery = early_recovery
            if day.end - day.start <= 3 * HOUR:
                recovery += quiet_bonus
        elif day.end <= OVERTIME_END:
            recovery = overtime_recovery
        owed = max(0, owed - recovery * HOUR)
    return states


def print_report(runs):
    print("| Seed | Archetype | Town | Profile | Days | OT h/wk | Late nights/wk | Long bus legs | WORN | SPENT |")
    print("|---|---|---|---|---:|---:|---:|---:|---:|---:|")
    for (seed, archetype, town, profile), days in sorted(runs.items()):
        weeks = len(days) / 7
        states = wake_states(days)
        print(f"| {seed} | {archetype} | {town} | {profile} | {len(days)} | "
              f"{sum(day.overtime for day in days) / HOUR / weeks:.2f} | "
              f"{sum(day.night > 0 for day in days) / weeks:.2f} | "
              f"{sum(day.long_bus_legs for day in days)} | {states['WORN']} | {states['SPENT']} |")

    print("\n| Recovery by 6 PM / by 9 PM (hours) | Busy WORN | Busy SPENT | Plain WORN | Plain SPENT |")
    print("|---|---:|---:|---:|---:|")
    for early, overtime in [(4, 2), (5, 2), (6, 2), (4, 1), (4, 0), (3, 2), (2, 2), (1, 2)]:
        counts = {"busy": Counter(), "plain": Counter()}
        for (_, _, _, profile), days in runs.items():
            states = wake_states(days, early_recovery=early, overtime_recovery=overtime)
            states["WORN"] -= 1  # Exclude the opening hangover from tuning.
            counts[profile].update(states)
        rates = []
        for profile in ("busy", "plain"):
            total = counts[profile].total()
            rates.extend(f"{counts[profile][state] / total:.1%}" if total else "—" for state in ("WORN", "SPENT"))
        print(f"| {early} / {overtime} | {' | '.join(rates)} |")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("patterns", nargs="+")
    patterns = parser.parse_args().patterns
    paths = sorted({path for pattern in patterns for path in glob(pattern)})
    if not paths:
        parser.error("no matching reports")
    runs = {}
    for path in paths:
        key, days = read_run(path)
        if key in runs:
            parser.error(f"duplicate run for {key}")
        runs[key] = days
    print_report(runs)


if __name__ == "__main__":
    main()
