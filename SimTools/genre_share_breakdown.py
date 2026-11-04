"""Per-genre share and year-end slot error by arm, read directly from each run's genre outputs.

Usage: genre_share_breakdown.py --tags TAG [TAG ...] --seeds 1001 2002 ... [--weeks 260] [--metric Share|Slots]
Shares come from {run}-genre-decade-shape.csv (every genre with units, all formats) against
AdjustedHistoricalGenreShareTargets.csv; slots from {run}-year-end-hot100.csv against the live ChartSlotBenchmark.
A genre absent from a run counts as zero there, so seed means are over all seeds (unlike the calibration
analyzer's per-genre rows, which exist only for genres that charted). Totals match its ShareError/SlotError.
"""
import argparse
import re
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parents[1]


def norm(value):
    value = re.sub(r"[^a-z0-9]", "", re.sub(r"\(.*?\)", "", value).lower())
    return {"rb": "rnb"}.get(value, value)


def share_targets():
    table = pd.read_csv(ROOT / "SimTools/AdjustedHistoricalGenreShareTargets.csv")
    return {(norm(row["genre"]), int(y)): row[y] for _, row in table.iterrows() for y in table.columns if y.isdigit()}


def slot_targets():
    text = (ROOT / "Data/GenreCatalog.cs").read_text(encoding="utf-8-sig")
    block = text.split("ChartSlotBenchmark = new() {", 1)[1].split("};", 1)[0]
    result = {}
    for genre, numbers in re.findall(r"\[Genre\.(\w+)\]\s*=\s*new\[\]\s*\{([^}]+)\}", block):
        for i, v in enumerate(int(x) for x in numbers.split(",") if x.strip()):
            result[(norm(genre), 1960 + i)] = v
    return result


def load(tag, seed, weeks, metric):
    prefix = ROOT / f"SimLogs/{tag}-{seed}-{weeks}"
    if metric == "Share":
        d = pd.read_csv(f"{prefix}-genre-decade-shape.csv")
        d["value"] = 100 * d.marketUnitsShare
    else:
        d = pd.read_csv(f"{prefix}-year-end-hot100.csv")
        d["value"] = d.yearEndSlots
    d["genre"] = d.genre.map(norm)
    return d.groupby(["year", "genre"]).value.sum()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--tags", nargs="+", required=True)
    parser.add_argument("--seeds", nargs="+", type=int, required=True)
    parser.add_argument("--weeks", type=int, default=260)
    parser.add_argument("--metric", choices=["Share", "Slots"], default="Share")
    parser.add_argument("--years", nargs=2, type=int, default=[1960, 1964])
    parser.add_argument("--floor", type=float, default=1.0, help="hide genres below this in target and every arm")
    args = parser.parse_args()
    targets = share_targets() if args.metric == "Share" else slot_targets()
    means = {}
    for tag in args.tags:
        runs = pd.concat([load(tag, s, args.weeks, args.metric) for s in args.seeds], axis=1).fillna(0)
        means[tag] = runs.mean(axis=1)
    frame = pd.DataFrame(means).fillna(0)
    for year in range(args.years[0], args.years[1] + 1):
        sub = frame.xs(year, level="year") if year in frame.index.get_level_values("year") else frame.iloc[0:0]
        genres = sorted(set(sub.index) | {g for g, y in targets if y == year and targets[(g, y)] > 0})
        sub = sub.reindex(genres).fillna(0)
        sub.insert(0, "target", [targets.get((g, year), 0) for g in genres])
        totals = " ".join(f"{t}={(sub[t] - sub['target']).abs().sum():.2f}" for t in args.tags)
        shown = sub[(sub.abs() >= args.floor).any(axis=1)]
        print(f"== {year} {args.metric} total abs error (of seed-mean values): {totals}")
        print(shown.round(2).to_string())


if __name__ == "__main__":
    main()
