"""Genre metric shift between two runs (B - A), per year 1960-1969.

usage: python genre_shift.py <runA> <runB> [--genres Funk,TeenPop,BritishPop]
                             [--metric chartWeekShare|marketUnitsShare|chartUnitsShare]
Positive delta = B has more than A. Default metric chartWeekShare.
"""
import sys, argparse
from _common import load_shape, norm

ap = argparse.ArgumentParser()
ap.add_argument("runA"); ap.add_argument("runB")
ap.add_argument("--genres", default="")
ap.add_argument("--metric", default="chartWeekShare")
a = ap.parse_args()

A = load_shape(a.runA); B = load_shape(a.runB)
want = [norm(g) for g in a.genres.split(",") if g.strip()]

def scale(shape, metric):
    s = [float(r[metric]) for r in shape.values()]
    return 100.0 if (s and max(s) <= 1.5) else 1.0

sA = scale(A, a.metric); sB = scale(B, a.metric)
genres = sorted({g for (_, g) in A.keys()} | {g for (_, g) in B.keys()})
if want:
    genres = [g for g in genres if g in want]

print(f"metric={a.metric}   A={a.runA}  B={a.runB}   (B - A, {a.metric} pts)")
hdr = "genre".ljust(16) + "".join(f"{y:>7d}" for y in range(1960, 1970)) + "   net"
print(hdr)
for g in genres:
    disp = next((A.get((y, g), B.get((y, g)))["_genre"] for y in range(1960, 1970)
                 if (y, g) in A or (y, g) in B), g)
    cells, net = [], 0.0
    for y in range(1960, 1970):
        ra = A.get((y, g)); rb = B.get((y, g))
        if ra is None and rb is None:
            cells.append("   -   "); continue
        va = float(ra[a.metric]) * sA if ra else 0.0
        vb = float(rb[a.metric]) * sB if rb else 0.0
        d = vb - va; net += d
        cells.append(f"{d:+7.2f}")
    print(disp.ljust(16) + "".join(cells) + f"  {net:+6.2f}")
