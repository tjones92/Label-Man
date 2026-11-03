"""Legacy lifecycle-based Phase 4 screen, retained to reproduce the original reports.

For historical calibration use --chart-history and analyze_follower_calibration.py:
this proxy infers dates from retirement and excludes titles still active at the endpoint.

Usage:  py SimTools/analyze_follower_demand.py <control-run> <treatment-run> [<control-run> <treatment-run> ...]

For each pair of 1963-66 resumes, treatment against control:
  repeat-hit share   Top 40 records (lifecycles peak <= 40, entry = close week - weeks on chart) whose act had another
                     Top 40 entry in the 104 weeks before, by entry year. Records still on the chart at the end are not
                     in lifecycles, so the last months undercount in both arms alike.
  runs               acts whose singles (single-launch-quality, in release order) include 3+ / 5+ straight Top 40s
  acts               distinct acts with a Top 40 entry, and the top 5% of those acts' share of Top 40 entries
  console            MEDIA_FOLLOWER_DEMAND (books, asked, granted, stocked, advance-order sizes, live) for the treatment
The economy table (concentration, Major share, CR4, genre, units, closures) is analyze_media_recognition_screen.py.
"""
import os
import re
import sys

import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")
YEARS = [1963, 1964, 1965, 1966]


def path(run, name):
    for d in (LOGS, os.path.join(LOGS, run)):
        p = os.path.join(d, f"{run}-{name}")
        if os.path.exists(p):
            return p
    return os.path.join(LOGS, f"{run}-{name}")


def hits(run):
    """Top 40 records with their act and entry week (audit week numbering)."""
    life = pd.read_csv(path(run, "lifecycles.csv"), usecols=["week", "recordId", "peakPosition", "weeksOnChart"])
    acts = pd.read_csv(path(run, "single-launch-quality.csv"), usecols=["week", "year", "singleId", "artistId"])
    life = life[(life.peakPosition >= 1) & (life.peakPosition <= 40)].copy()
    life["entry"] = life.week - life.weeksOnChart
    life = life.merge(acts.rename(columns={"singleId": "recordId", "week": "released", "year": "relYear"}), on="recordId", how="inner")
    weeks = pd.read_csv(path(run, "weeks.csv"), usecols=["week", "year"]).drop_duplicates("week")
    life = life.merge(weeks.rename(columns={"week": "entry", "year": "entryYear"}), on="entry", how="left")
    return life.sort_values(["artistId", "entry", "recordId"]), acts


def repeat_share(h):
    rows = {}
    for year in YEARS:
        cur = h[h.entryYear == year]
        if cur.empty:
            continue
        repeat = 0
        for _, r in cur.iterrows():
            prior = h[(h.artistId == r.artistId) & (h.recordId != r.recordId) & (h.entry < r.entry) & (h.entry >= r.entry - 104)]
            repeat += 1 if len(prior) else 0
        rows[year] = (len(cur), repeat / len(cur))
    return rows


def runs(h, acts):
    top = set(h.recordId)
    best = {}
    for artist, g in acts.sort_values(["artistId", "week", "singleId"]).groupby("artistId"):
        streak = longest = 0
        for rid in g.singleId:
            streak = streak + 1 if rid in top else 0
            longest = max(longest, streak)
        best[artist] = longest
    s = pd.Series(best)
    return int((s >= 3).sum()), int((s >= 5).sum()), int(s.max()) if len(s) else 0


def spread(h):
    per = h.groupby("artistId").size().sort_values(ascending=False)
    k = max(1, int(round(0.05 * len(per))))
    return len(per), per.head(k).sum() / per.sum() if per.sum() else 0.0


def follower_lines(run):
    p = path(run, "console.log")
    if not os.path.exists(p):
        return []
    with open(p, encoding="utf-8", errors="replace") as f:
        return [line.strip() for line in f if line.startswith("MEDIA_FOLLOWER_DEMAND") or line.startswith("MEDIA_FOLLOWING year")]


def pair(ctl, trt):
    print(f"\n=== {trt} against {ctl}")
    hc, ac = hits(ctl)
    ht, at = hits(trt)
    rc, rt = repeat_share(hc), repeat_share(ht)
    print("repeat-hit share (Top 40 entries by acts with a Top 40 in the prior 104 weeks):")
    for year in YEARS:
        if year in rc and year in rt:
            print(f"  {year}: control {rc[year][1]:.1%} of {rc[year][0]}  treatment {rt[year][1]:.1%} of {rt[year][0]}  ({(rt[year][1] - rc[year][1]) * 100:+.1f} pt)")
    for label, (h, a) in (("control", (hc, ac)), ("treatment", (ht, at))):
        r3, r5, longest = runs(h, a)
        n, top5 = spread(h)
        print(f"  {label:9s}: Top 40 records {len(h)}, acts {n}, top 5% of acts {top5:.1%} of entries; straight Top 40 runs 3+ {r3}, 5+ {r5}, longest {longest}")
    for line in follower_lines(trt):
        print("  " + line[:700])


def main():
    args = sys.argv[1:]
    if len(args) < 2 or len(args) % 2:
        sys.exit(__doc__)
    print('Legacy lifecycle proxy: use analyze_follower_calibration.py with complete --chart-history runs for historical targets.')
    for i in range(0, len(args), 2):
        pair(args[i], args[i + 1])


if __name__ == "__main__":
    main()
