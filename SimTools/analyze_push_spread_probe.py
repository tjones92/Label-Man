"""Paired 1960 comparison for the --radio-push-spread probe (HistoricalLabelTierBalanceResearch.md §10).

Reads 52-week audit logs only. For each seed, compares a control run with a probe run on:
Top 10 slot concentration (CR4/CR8, the historical review measure), Big-5 and tier shares, and the
chart-health guards used across the audits (units, entries, chart life, #1 count/tenure/returns,
debut position, quality/outcome correlation). Seeds are reported separately and as an unweighted mean.

Usage: python analyze_push_spread_probe.py --control PREFIX_FMT --probe PREFIX_FMT --seeds 1001 2002 ...
       --output OUT.json   (PREFIX_FMT contains {seed}, e.g. SimLogs/push-spread-050-default-{seed}-52)
"""
import argparse
import json
from pathlib import Path
from statistics import mean

import numpy as np
import pandas as pd

BIG5 = {"label_0001", "label_0002", "label_0003", "label_0004", "label_0005"}
TIERS = ["Major", "MidTier", "Independent", "Boutique", "Small"]
HISTORY_1960 = {"cr4": 28.0, "cr8": 52.0, "cr8MinusCr4": 24.0}


def spells(leaders):
    out, cur, n = [], None, 0
    for x in leaders:
        if x == cur:
            n += 1
        else:
            if cur is not None:
                out.append((cur, n))
            cur, n = x, 1
    out.append((cur, n))
    return pd.DataFrame(out, columns=["rec", "len"])


def metrics(prefix):
    cols = ["week", "year", "recordId", "labelId", "labelTier", "genre", "quality",
            "currentPosition", "unitsThisWeek", "weeksSinceRelease"]
    parts = [c[c.year.eq(1960) & c.currentPosition.between(1, 100)]
             for c in pd.read_csv(prefix + "-records.csv", usecols=cols, chunksize=200000)]
    rec = pd.concat(parts, ignore_index=True)
    top = rec[rec.currentPosition.le(10)]
    assert len(top) == 520 and top.week.nunique() == 52, prefix
    slots = top.groupby("labelId").size().sort_values(ascending=False)
    per = rec.groupby("recordId").agg(lab=("labelId", "first"), tier=("labelTier", "first"),
                                      q=("quality", "first"), peak=("currentPosition", "min"),
                                      units=("unitsThisWeek", "sum"))
    per["b5"] = per.lab.isin(BIG5)
    per["t10"] = per.peak.le(10)
    upper = per.q.ge(per.q.median())
    # weeks.csv is header-only under --calibration, so #1s and entries come from records.csv.
    no1 = rec[rec.currentPosition.eq(1)].sort_values("week")
    assert no1.week.is_unique and len(no1) == 52, prefix
    sp = spells(list(no1.recordId))
    entry_week = rec.groupby("recordId").week.min()
    top40_week = rec[rec.currentPosition.le(40)].groupby("recordId").week.min()
    per_no1 = sp.groupby("rec").len.agg(["size", "sum"])
    first = rec.sort_values("week").groupby("recordId").first()
    debut = first[(first.week > 1) & (first.weeksSinceRelease >= 1)].currentPosition
    lc = pd.read_csv(prefix + "-lifecycles.csv")
    lc = lc[(~lc.leftCensoredAtRunStart.astype(str).str.lower().eq("true")) & (lc.peakPosition >= 1)]
    roll = pd.read_csv(prefix + "-decade-annual-rollup.csv").set_index("year")
    cr4 = 100 * slots.head(4).sum() / 520
    cr8 = 100 * slots.head(8).sum() / 520
    return {
        "cr4": cr4, "cr8": cr8, "cr8MinusCr4": cr8 - cr4,
        "leader": f"{slots.index[0]} {100 * slots.iloc[0] / 520:.1f}%",
        "labelsWithTop10": int(len(slots)),
        "big5Top10SlotPct": 100 * top.labelId.isin(BIG5).mean(),
        "big5Hot100EntryPct": 100 * per.b5.mean(),
        "big5Hot100ToTop10Pct": 100 * per[per.b5].t10.mean(),
        "otherHot100ToTop10Pct": 100 * per[~per.b5].t10.mean(),
        "big5UpperQualityToTop10Pct": 100 * per[per.b5 & upper].t10.mean(),
        "otherUpperQualityToTop10Pct": 100 * per[~per.b5 & upper].t10.mean(),
        **{f"top10Slot{t}Pct": 100 * top.labelTier.eq(t).mean() for t in TIERS},
        **{f"hot100Entry{t}Pct": 100 * per.tier.eq(t).mean() for t in TIERS},
        # guards
        "singleUnitsM": float(roll.loc[1960, "singleUnits"]) / 1e6,
        "chartUnitsM": float(rec.unitsThisWeek.sum()) / 1e6,
        "hot100Records": int(len(per)),
        "newEntriesPerWeek": float(entry_week.gt(1).sum() / 51),
        "newTop40PerWeek": float(top40_week.gt(1).sum() / 51),
        "closedChartLifeMean": float(lc.weeksOnChart.mean()),
        "distinctNo1": int(len(per_no1)),
        "no1MeanTenure": float(per_no1["sum"].mean()),
        "no1SpellLen": float(sp.len.mean()),
        "no1ReturnPct": float(100 * (per_no1["size"] >= 2).mean()),
        "debutMean": float(debut.mean()),
        "debutTop40Pct": float(100 * debut.le(40).mean()),
        "qualityPeakSpearman": float(-per.q.rank().corr(per.peak.rank())),
        "qualityUnitsPearson": float(np.corrcoef(per.q, np.log1p(per.units))[0, 1]),
        "distinctTop10Records": int(top.recordId.nunique()),
    }


def resolve(formats, seed):
    found = [f.format(seed=seed) for f in formats if Path(f.format(seed=seed) + "-records.csv").exists()]
    if not found:
        raise FileNotFoundError(f"no run for seed {seed} in {formats}")
    return found[0]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--control", nargs="+", required=True, help="one or more {seed} formats; the first whose records.csv exists is used")
    ap.add_argument("--probe", nargs="+", required=True)
    ap.add_argument("--seeds", nargs="+", type=int, required=True)
    ap.add_argument("--output", required=True)
    a = ap.parse_args()
    rows = {}
    for seed in a.seeds:
        rows[seed] = {"control": metrics(resolve(a.control, seed)), "probe": metrics(resolve(a.probe, seed))}
    keys = [k for k, v in rows[a.seeds[0]]["control"].items() if not isinstance(v, str)]
    summary = {}
    for k in keys:
        c = [rows[s]["control"][k] for s in a.seeds]
        p = [rows[s]["probe"][k] for s in a.seeds]
        d = [y - x for x, y in zip(c, p)]
        summary[k] = {"control": mean(c), "probe": mean(p), "delta": mean(d),
                      "deltaMin": min(d), "deltaMax": max(d),
                      "deltaSameSign": all(x > 0 for x in d) or all(x < 0 for x in d)}
    out = {"history1960": HISTORY_1960, "seeds": a.seeds, "perSeed": rows, "meanOverSeeds": summary}
    Path(a.output).parent.mkdir(parents=True, exist_ok=True)
    Path(a.output).write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
    print(f"{'metric':<30}{'control':>10}{'probe':>10}{'delta':>9}{'min':>8}{'max':>8}  same-sign")
    for k, v in summary.items():
        print(f"{k:<30}{v['control']:>10.2f}{v['probe']:>10.2f}{v['delta']:>9.2f}"
              f"{v['deltaMin']:>8.2f}{v['deltaMax']:>8.2f}  {'yes' if v['deltaSameSign'] else ''}")
    print("\nper-seed CR4 / CR8 (control -> probe), leader:")
    for s in a.seeds:
        c, p = rows[s]["control"], rows[s]["probe"]
        print(f"  {s}: {c['cr4']:.2f}/{c['cr8']:.2f} -> {p['cr4']:.2f}/{p['cr8']:.2f}   "
              f"{c['leader']} -> {p['leader']}")


if __name__ == "__main__":
    main()
