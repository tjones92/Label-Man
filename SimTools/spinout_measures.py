"""Phase 4d measures (SimTools/BandMemberSimulationDirective.md §4.15, §11): solo spin-outs and how they fare.

Usage:  py SimTools/spinout_measures.py <run> [<run> ...] [--min-year=1965]

Per run, over the years from --min-year: spin-outs by year and cause, the share of top-40 group acts that lost a
member to a solo career (against Data/LineupReferenceSet.csv), whether each spin-out act charted (any record in
first-chart-events, mapped through live-records-snapshot) and its debut position, substance deaths, and member fame
quantiles at the run's end. Diagnostic output only.
"""
import csv
import os
import sys
from collections import Counter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")


def read(name):
    path = os.path.join(LOGS, name)
    if not os.path.exists(path):
        return []
    with open(path, encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f))


def reference_share():
    with open(os.path.join(ROOT, "Data", "LineupReferenceSet.csv"), encoding="utf-8", newline="") as f:
        rows = [r for r in csv.DictReader(f) if r["type"] != "Solo"]
    return sum(r["soloSpinout"] == "Y" for r in rows) / max(1, len(rows)), len(rows)


def quantiles(values, qs):
    values = sorted(values)
    if not values:
        return {q: 0.0 for q in qs}
    return {q: values[min(len(values) - 1, int(q * len(values)))] for q in qs}


def analyze(run, min_year):
    events = [e for e in read(f"{run}-lineup-events.csv") if int(e["year"]) >= min_year]
    spins = [e for e in events if e["event"] == "solo-spinout"]
    print(f"=== {run} (from {min_year}) ===")
    print(f"spin-outs {len(spins)}  by year {dict(sorted(Counter(e['year'] for e in spins).items()))}")
    print(f"  by cause {dict(Counter(e['cause'] for e in spins))}  from charted acts {sum(e['everCharted'] == 'true' for e in spins)}")

    # Top-40 group acts in the window: lineup events carry no top-40 flag, so use chartedThisYear on group acts as
    # the in-window charting population and report the reference share beside it.
    charted_groups = {e["artistId"] for e in events if e["chartedThisYear"] == "true" and e["constitution"] != "Solo"}
    donors = {e["otherPersonId"] for e in spins}
    ref, n = reference_share()
    print(f"  group acts charting in-window with any lineup event {len(charted_groups)}; donor acts {len(donors)}"
          f" ({len(donors & charted_groups) / max(1, len(charted_groups)):.1%} of them)  ref: {ref:.1%} of {n} top-40 groups")

    solo_ids = {e["artistId"] for e in spins}
    records = {}
    for r in read(f"{run}-live-records-snapshot.csv"):
        if r["artistId"] in solo_ids:
            records.setdefault(r["recordId"], r["artistId"])
    charted = {}
    for r in read(f"{run}-first-chart-events.csv"):
        a = records.get(r["recordId"])
        if a and int(r["year"]) >= min_year:
            charted[a] = min(charted.get(a, 999), int(r["currentPosition"]))
    debuts = sorted(charted.values())
    print(f"  spin-out acts that charted {len(charted)}/{len(solo_ids)}; best debut positions {debuts[:15]}")

    annual = [r for r in read(f"{run}-lineup-annual.csv") if int(r["year"]) >= min_year]
    subst = sum(int(r["deathSubstance"]) for r in annual)
    print(f"  substance onsets {sum(int(r['substanceOnsets']) for r in annual)}  substance deaths {subst}"
          f"  deaths charting {sum(int(r['deathsCharting']) for r in annual)}  leaving-member options"
          f" {sum(int(r['leavingMemberOptions']) for r in annual)}")

    fame = [float(r["personalRecognition"]) for r in read(f"{run}-musician-recognition.csv")]
    q = quantiles(fame, [0.5, 0.9, 0.99, 0.999])
    print(f"  member fame at end: n {len(fame)}  >=.05 {sum(f >= 0.05 for f in fame)}  max {max(fame, default=0):.3f}"
          f"  q50/90/99/99.9 {q[0.5]:.4f}/{q[0.9]:.4f}/{q[0.99]:.4f}/{q[0.999]:.4f}")
    print()


def main():
    min_year = 1965
    runs = []
    for a in sys.argv[1:]:
        if a.startswith("--min-year="):
            min_year = int(a.split("=", 1)[1])
        else:
            runs.append(a)
    for run in runs:
        analyze(run, min_year)


if __name__ == "__main__":
    main()
