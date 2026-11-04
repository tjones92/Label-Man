"""Compare chart-shape probe runs with a paired control (HistoricalLabelTierBalanceResearch.md §17d-e):
--airplay-rotation-ceiling and --chart-inertia-base.

Reads logs only. Requires pandas. Each argument is a run prefix such as SimLogs/rotceil-027-default-1001-52; the first
is the control. With --identity A B, also hashes every CSV of run A against run B to prove a switch-off build inert.
Reports the Top 10 shape the ceiling targets (published #1/#10, peaks per rank, distinct Top 10 records, weeks per
record, Top 10 visitors that leave after a week, Top 10 slot-weeks held by the inertia cap) and the guards (#1, chart life, entries, genre, Big-5, CR4/CR8,
single units). Within a 52-week run the #1 statistics are noise (52-week-probes-cannot-resolve-no1-tenure).
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
import pandas as pd

BIG5 = {f"label_000{i}" for i in range(1, 6)}


def r3(v):
    return None if v is None or (isinstance(v, float) and np.isnan(v)) else round(float(v), 3)


def identity(a, b):
    files_a = {p.name[len(Path(a).name):]: p for p in Path(a).parent.glob(Path(a).name + "-*.csv")}
    files_b = {p.name[len(Path(b).name):]: p for p in Path(b).parent.glob(Path(b).name + "-*.csv")}
    digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
    shared = sorted(set(files_a) & set(files_b))
    differ = [s for s in shared if digest(files_a[s]) != digest(files_b[s])]
    return dict(shared=len(shared), onlyInA=sorted(set(files_a) - set(files_b)), onlyInB=sorted(set(files_b) - set(files_a)),
                differ=differ, identical=not differ and len(shared) > 0)


def run_metrics(prefix):
    r = pd.read_csv(prefix + "-records.csv")
    r = r[r.currentPosition > 0].copy()
    last = int(r.week.max())
    r["big5"] = r.labelId.isin(BIG5)
    top = r[r.currentPosition.le(10)]
    ratio = top.pivot_table(index="week", columns="currentPosition", values="chartPoints")
    span = r.groupby("recordId").week.agg(["min", "max"])
    best = r.groupby("recordId").currentPosition.min()
    unc_best = best[(span["min"] > 1) & (span["max"] < last)]
    per = top.groupby("recordId").agg(w=("week", "size"), q=("quality", "first"), big5=("big5", "first"),
                                      best=("currentPosition", "min"), first=("week", "min"), last=("week", "max"))
    unc = per[(per["first"] > 1) & (per["last"] < last)]
    t = top.sort_values(["recordId", "week"])
    nxt = t.groupby("recordId").shift(-1)
    entry = t[(t.week == t.recordId.map(per["first"])) & (t.week > 1)]
    e810 = entry[entry.currentPosition.between(8, 10)]
    gone = 100 * (~(nxt.loc[e810.index].week == e810.week + 1)).mean()
    one = t[t.currentPosition == 1]
    spells = one.groupby("recordId").week.apply(lambda w: 1 + int((np.diff(w.values) > 1).sum()))
    lc = pd.read_csv(prefix + "-lifecycles.csv")
    lc = lc[(lc.leftCensoredAtRunStart.astype(str).str.lower() != "true") & (lc.weeksOnChart > 0)]
    rollup = pd.read_csv(prefix + "-decade-annual-rollup.csv")
    firm = top.groupby("labelId").size().sort_values(ascending=False)
    upper = per.q >= per.q.median()
    genre = top.groupby("genre").size() / len(top) * 100
    shape = pd.read_csv(prefix + "-genre-decade-shape.csv")
    air = (r.unsampledChartPoints - r.unitsThisWeek).clip(lower=0)
    pts = np.where(r.unitsThisWeek == 0, r.chartPoints * 0.1, r.chartPoints)   # ChartManager step 4
    raw = pd.Series(pts, index=r.index).groupby(r.week).rank(ascending=False, method="first")
    denied = (raw.groupby(r.recordId).min() <= 10) & (best > 10)
    return dict(
        rank1OverRank10=r3((ratio[1] / ratio[10]).median()),
        rank1OverRank2=r3((ratio[1] / ratio[2]).median()),
        rank5OverRank10=r3((ratio[5] / ratio[10]).median()),
        airShareAtRank1=r3((air / r.unsampledChartPoints)[r.currentPosition == 1].median()),
        airShareAtRank10=r3((air / r.unsampledChartPoints)[r.currentPosition == 10].median()),
        top10SlotsHeldByInertiaPct=r3(100 * (raw[r.currentPosition.le(10)] > 10).mean()),
        recordsDeniedTop10=int(denied.sum()),
        top10RecordsByYear={int(y): int(g.recordId.nunique()) for y, g in top.groupby("year")},
        top10WeeksPerRecordByYear={int(y): r3(len(g) / g.recordId.nunique()) for y, g in top.groupby("year")},
        no1Spell=r3(len(one) / max(1, spells.sum())),
        top10Records=len(per), top10WeeksPerRecord=r3(per.w.mean()), uncensoredWeeksPerRecord=r3(unc.w.mean()),
        peaks1to3=int(unc_best.between(1, 3).sum()), peaks4to6=int(unc_best.between(4, 6).sum()),
        peaks7to10=int(unc_best.between(7, 10).sum()), peaks11to14=int(unc_best.between(11, 14).sum()),
        peakCountByRank={int(k): int(v) for k, v in unc_best.value_counts().sort_index().loc[1:14].items()},
        entries8to10=len(e810), entries8to10GoneNextWeekPct=r3(gone),
        no1Records=len(spells), no1Tenure=r3(len(one) / max(1, len(spells))), no1ReturnsPct=r3(100 * (spells > 1).mean()),
        closedChartLife=r3(lc.weeksOnChart.mean()),
        hot100EntriesPerWeek=r3((r.groupby("recordId").week.min() > 1).sum() / (last - 1)),
        top40EntriesPerWeek=r3((r[r.currentPosition.le(40)].groupby("recordId").week.min() > 1).sum() / (last - 1)),
        singleUnits=int(rollup.singleUnits.sum()),
        cr4=r3(100 * firm.iloc[:4].sum() / len(top)), cr8=r3(100 * firm.iloc[:8].sum() / len(top)),
        big5Top10SlotPct=r3(100 * top.big5.mean()),
        big5UpperWeeks=r3(per[per.big5 & upper].w.mean()), othersUpperWeeks=r3(per[~per.big5 & upper].w.mean()),
        top10GenreSlotPct={g: r3(v) for g, v in genre.sort_values(ascending=False).head(10).items()},
        chartWeekShareByGenre={g: r3(v) for g, v in shape.groupby("genre").chartWeekShare.sum().items()},
        marketShareByGenre={g: r3(v) for g, v in shape.groupby("genre").marketUnitsShare.sum().items()},
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+", help="run prefixes; the first is the control")
    parser.add_argument("--identity", nargs=2, metavar=("A", "B"))
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    result = {}
    if args.identity:
        result["identity"] = identity(*args.identity)
    runs = {Path(p).name: run_metrics(p) for p in args.prefixes}
    control = runs[Path(args.prefixes[0]).name]
    for name, m in runs.items():
        cw = control["chartWeekShareByGenre"]
        m["genreChartWeekShareAbsDeltaVsControl"] = r3(sum(abs(m["chartWeekShareByGenre"].get(g, 0) - v) for g, v in cw.items()))
        m["singleUnitsVsControlPct"] = r3(100 * (m["singleUnits"] / control["singleUnits"] - 1))
    result["runs"] = runs
    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    if args.identity:
        i = result["identity"]
        print(f"identity: {'IDENTICAL' if i['identical'] else 'DIFFERENT'} shared={i['shared']} differ={i['differ']} "
              f"onlyA={i['onlyInA']} onlyB={i['onlyInB']}")
    keys = ["top10SlotsHeldByInertiaPct", "recordsDeniedTop10", "rank1OverRank10", "rank1OverRank2", "rank5OverRank10", "airShareAtRank1", "top10Records", "top10WeeksPerRecord",
            "uncensoredWeeksPerRecord", "peaks1to3", "peaks4to6", "peaks7to10", "peaks11to14", "entries8to10GoneNextWeekPct",
            "no1Records", "no1Tenure", "no1Spell", "no1ReturnsPct", "closedChartLife", "hot100EntriesPerWeek", "top40EntriesPerWeek",
            "singleUnitsVsControlPct", "cr4", "cr8", "big5Top10SlotPct", "big5UpperWeeks", "othersUpperWeeks",
            "genreChartWeekShareAbsDeltaVsControl"]
    table = pd.DataFrame({name: {k: m[k] for k in keys} for name, m in runs.items()})
    print(table.to_string())
    for key in ("top10RecordsByYear", "top10WeeksPerRecordByYear"):
        print(key); print(pd.DataFrame({name: m[key] for name, m in runs.items()}).to_string())
    print(pd.DataFrame({name: m["top10GenreSlotPct"] for name, m in runs.items()}).round(1).fillna(0).to_string())


if __name__ == "__main__":
    main()
