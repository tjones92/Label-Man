"""Genre chart-vs-market divergence and longevity index, from genre-decade-shape.csv.

Never use genre-market-weekly.csv for chart share -- it is region-scoped (handoff 3.1).
"""
import sys, pandas as pd
BASE = r"C:\Project\Label-Man\SimLogs"
YEARS = [1963, 1966, 1967, 1968, 1969]
WATCH = ["Soul", "Gospel", "Country", "RnB", "PsychedelicRock", "Bubblegum"]
frames = {}
for run in sys.argv[1:]:
    d = pd.read_csv(rf"{BASE}\{run}-genre-decade-shape.csv")
    d = d[d.year != 1970]
    d["divg"] = d.chartWeekShare * 100 - d.marketUnitsShare * 100
    chart_mean = d.groupby("year").apply(
        lambda g: g.chartRecordWeeks.sum() / max(1, g.uniqueChartingRecords.sum()), include_groups=False)
    d["lon"] = d.apply(lambda r: (r.chartRecordWeeks / r.uniqueChartingRecords / chart_mean[r.year])
                       if r.uniqueChartingRecords > 0 else float("nan"), axis=1)
    frames[run] = d
    print(f"\n{run}: chart-wide mean weeks per charting record")
    print("  " + "  ".join(f"{y}:{chart_mean[y]:.2f}" for y in sorted(chart_mean.index)))
for genre in WATCH:
    print(f"\n=== {genre}")
    print(f"{'run':<32}" + "".join(f"{y:>9}" for y in YEARS))
    for run, d in frames.items():
        s = d[d.genre == genre].set_index("year")
        div = "".join(f"{s.divg[y]:>+9.1f}" if y in s.index else f"{'-':>9}" for y in YEARS)
        print(f"{run + ' div':<32}{div}")
    for run, d in frames.items():
        s = d[d.genre == genre].set_index("year")
        lon = "".join(f"{s.lon[y]:>9.2f}" if y in s.index else f"{'-':>9}" for y in YEARS)
        print(f"{run + ' longevity':<32}{lon}")
