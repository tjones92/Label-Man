"""Phase 4 world-churn A/B (SimTools/BandMemberSimulationDirective.md §7 measures 4-5) against an observe-scope control.

Usage:  py SimTools/band_life_ab.py <control-run> <world-run> [--max-year=1967]

An --observe-band-life run is byte-identical to a no-flag run on the economy, so it serves as the control. Compares,
over the years both runs completed: genre-share sumAbsErr vs the target table, year-end Hot 100 slot error, album unit
share, owner-Major chart-entry share, and population (acts in scope, formations per year). Diagnostic only.
"""
import os
import sys
from collections import Counter

import pandas as pd

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "radio-compare"))
from _common import SIMLOGS, load_shape, load_targets  # noqa: E402

BENCH_SLOTS = None


def slot_bench():
    global BENCH_SLOTS
    if BENCH_SLOTS is None:
        import importlib.util
        path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "radio-compare", "slot_error_compare.py")
        src = open(path, encoding="utf-8").read()
        start = src.index("BENCH = {")
        end = src.index("}\n", start) + 1
        ns = {}
        exec(src[start:end], ns)
        BENCH_SLOTS = ns["BENCH"]
    return BENCH_SLOTS


def share_err(run, max_year):
    shape, tg = load_shape(run), load_targets()
    per_year = Counter()
    per_genre = Counter()
    for (y, g), target in tg.items():
        if y > max_year:
            continue
        row = shape.get((y, g))
        if row is None:
            continue
        e = abs(float(row["marketUnitsShare"]) * 100.0 - target)
        per_year[y] += e
        per_genre[g] += e
    return per_year, per_genre


def slot_err(run, max_year):
    d = pd.read_csv(os.path.join(SIMLOGS, f"{run}-year-end-hot100.csv"))
    t = d.pivot_table(index="genre", columns="year", values="yearEndSlots", aggfunc="sum").fillna(0)
    bench = slot_bench()
    err = Counter()
    for y in range(1960, max_year + 1):
        for g in set(bench) | set(t.index):
            b = bench.get(g, [0] * 10)[y - 1960]
            v = t.loc[g, y] if (g in t.index and y in t.columns) else 0
            err[y] += abs(v - b)
    return err


def rollup(run):
    r = pd.read_csv(os.path.join(SIMLOGS, f"{run}-decade-annual-rollup.csv")).set_index("year")
    return (r["albumUnits"] / (r["albumUnits"] + r["singleUnits"]) * 100).round(2)


def owner_major(run):
    c = pd.read_csv(os.path.join(SIMLOGS, f"{run}-concentration.csv")).set_index("year")
    return (c["ownerMajorEntries"] / c["chartEntries"] * 100).round(2)


def population(run):
    a = pd.read_csv(os.path.join(SIMLOGS, f"{run}-lineup-annual.csv")).set_index("year")
    ev = pd.read_csv(os.path.join(SIMLOGS, f"{run}-artist-population-events.csv"), usecols=["eventType", "date"], on_bad_lines="skip", engine="python")
    ev["year"] = ev["date"].str.strip('"').str.split("/").str[-1].astype(int)
    form = ev[ev["eventType"] == "formation"].groupby("year").size()
    return a[["actsInScope", "groupActs"]].assign(formations=form)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    max_year = int(next((a.split("=")[1] for a in sys.argv[1:] if a.startswith("--max-year=")), 1967))
    ctl, world = args
    print(f"=== {world} vs control {ctl}, 1960-{max_year} ===")
    (sy_c, sg_c), (sy_w, sg_w) = share_err(ctl, max_year), share_err(world, max_year)
    print(f"\ngenre-share sumAbsErr: control {sum(sy_c.values()):.1f}  world {sum(sy_w.values()):.1f}  "
          f"delta {sum(sy_w.values()) - sum(sy_c.values()):+.1f}")
    print("  by year: " + "  ".join(f"{y}:{sy_w[y] - sy_c[y]:+.1f}" for y in sorted(sy_c)))
    worst = sorted(((sg_w[g] - sg_c[g], g) for g in sg_c), reverse=True)
    print("  largest per-genre moves: " + ", ".join(f"{g} {d:+.1f}" for d, g in worst[:5]) + " | " +
          ", ".join(f"{g} {d:+.1f}" for d, g in worst[-3:]))
    e_c, e_w = slot_err(ctl, max_year), slot_err(world, max_year)
    print(f"\nyear-end slot error: control {sum(e_c.values()):.0f}  world {sum(e_w.values()):.0f}  "
          f"delta {sum(e_w.values()) - sum(e_c.values()):+.0f}")
    lp = pd.DataFrame({"control": rollup(ctl), "world": rollup(world)}).dropna()
    print("\nalbum unit share % (by year):\n" + lp.T.to_string())
    om = pd.DataFrame({"control": owner_major(ctl), "world": owner_major(world)}).dropna()
    print("\nowner-Major chart-entry share %:\n" + om.T.to_string())
    pc, pw = population(ctl), population(world)
    print("\npopulation (control / world):")
    for y in pc.index.intersection(pw.index):
        print(f"  {y}: acts {pc.loc[y, 'actsInScope']:>6} / {pw.loc[y, 'actsInScope']:<6}  groups {pc.loc[y, 'groupActs']:>6} / "
              f"{pw.loc[y, 'groupActs']:<6}  formations {pc.loc[y, 'formations']:>5} / {pw.loc[y, 'formations']}")


if __name__ == "__main__":
    main()
