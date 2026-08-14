"""Per-genre marketUnitsShare vs historical targets for one run.

usage: python target_check.py <run> [--genres Funk,TeenPop,BritishPop] [--metric marketUnitsShare]
Prints, for the requested genres (default: all with a target), the run share,
target share, and delta per year 1960-1969, plus a sum-of-abs-error total.
"""
import sys, argparse
from _common import load_shape, load_targets, norm

ap = argparse.ArgumentParser()
ap.add_argument("run")
ap.add_argument("--genres", default="")
ap.add_argument("--metric", default="marketUnitsShare",
                choices=["marketUnitsShare", "chartWeekShare", "chartUnitsShare"])
a = ap.parse_args()

shape = load_shape(a.run)
tg = load_targets()
want = [norm(g) for g in a.genres.split(",") if g.strip()]

# scale: marketUnitsShare is a 0..1 fraction; targets are percents.
def run_pct(row):
    v = float(row[a.metric])
    return v * 100.0 if a.metric == "marketUnitsShare" else v  # chart shares already pct? check below

# detect chart share scale (0..1 vs 0..100) from data max
sample = [float(r[a.metric]) for r in shape.values()]
mx = max(sample) if sample else 1
scale100 = mx <= 1.5

genres = sorted({g for (_, g) in tg.keys()})
if want:
    genres = [g for g in genres if g in want]

print(f"run={a.run}  metric={a.metric}")
grand = 0.0
for g in genres:
    disp = next((shape[(y, g)]["_genre"] for y in range(1960, 1970) if (y, g) in shape), g)
    line, ae = [], 0.0
    for y in range(1960, 1970):
        t = tg.get((y, g))
        row = shape.get((y, g))
        if t is None or row is None:
            line.append(f"{y}:   -   ")
            continue
        v = float(row[a.metric]) * (100.0 if scale100 else 1.0)
        d = v - t
        ae += abs(d)
        line.append(f"{y}:{v:5.1f}/{t:4.1f}({d:+4.1f})")
    grand += ae
    print(f"\n{disp:16s} sumAbsErr={ae:5.1f}")
    for i in range(0, 10, 5):
        print("   " + "  ".join(line[i:i+5]))
print(f"\nTOTAL sumAbsErr (listed genres) = {grand:.1f}")
