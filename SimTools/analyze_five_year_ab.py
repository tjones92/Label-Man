"""Five-year A/B for the Small-label bundle (SmallLabelAttritionAndChartMethod.md §11), by year.

Usage: python SimTools/analyze_five_year_ab.py SimLogs/<control> SimLogs/<treatment> [SimLogs/<control2> SimLogs/<treatment2> ...]

Starting cohorts are the labels in each tier at week 1 of label-finance; a promotion out of the tier is a survivor.
Reads label-finance, deal-ledger, independent-distribution-events, records, completed-week-settlement(-regional),
release-outcomes, concentration and label-geography. Regional top-11 overlap comes from analyze_regional_sync.py.
Read-only.
"""
import json, os, subprocess, sys
import numpy as np, pandas as pd

CLOSED = ["Defunct", "Bankrupt"]
HERE = os.path.dirname(os.path.abspath(__file__))
YEARS = range(1960, 1965)


def year_of(week):
    return 1960 + (np.asarray(week) - 1) // 52


def arm(prefix):
    r = {}
    fin = pd.read_csv(prefix + "-label-finance.csv", usecols=lambda c: c in [
        "week", "year", "labelId", "labelTier", "status", "cashReserves", "weeklyPressing", "weeklyPlantPaid",
        "outstandingPlantPayables", "outstandingWholesaleReceivables"])
    w1 = fin[fin.week == 1].set_index("labelId")
    tier0 = w1.labelTier
    small = set(w1.index[tier0 == "Small"])
    transitions = pd.read_csv(prefix + "-label-tier-transitions.csv")
    promotions = transitions[(transitions.direction == "promotion") & transitions.labelId.isin(small)]
    first_promotion = promotions.groupby("labelId").week.min()
    first_closure = fin[fin.status.isin(CLOSED) & fin.labelId.isin(small)].groupby("labelId").week.min()
    for y in YEARS:
        g = fin[fin.year <= y]
        end = g.sort_values("week").groupby("labelId").last()
        start = end[end.index.isin(w1.index)]
        for t in ("Small", "Boutique", "Independent", "MidTier", "Major"):
            e = start[start.index.map(tier0) == t]
            r[f"{t} closed cum {y} (%)"] = 100 * e.status.isin(CLOSED).mean()
        # Keep the original all-firm cohort metric above. The documented Small
        # attrition target treats first promotion as a successful exit from Small;
        # bankruptcy later as an Independent is a different outcome.
        cutoff = int(g.week.max())
        small_failure = first_closure[(first_closure <= cutoff) &
            (first_closure < first_promotion.reindex(first_closure.index).fillna(float("inf")))]
        r[f"Small financial exits before promotion cum {y} (%)"] = 100 * len(small_failure) / max(1, len(small))
        r[f"Small first promotions cum {y} (n)"] = int((first_promotion <= cutoff).sum())
        alive = start[(start.index.map(tier0) == "Small") & ~start.status.isin(CLOSED + ["Acquired"])]
        r[f"Small survivors still Small / promoted {y}"] = "%d / %d" % ((alive.labelTier == "Small").sum(), (alive.labelTier != "Small").sum())
        r[f"Small survivor median cash {y}"] = float(alive.cashReserves.median())
    first = fin.sort_values("week").groupby("labelId").first()
    entrants = first[first.week > 1]
    last = fin.sort_values("week").groupby("labelId").last()
    r["entrants / closed by run end"] = "%d / %d" % (len(entrants), last.loc[entrants.index].status.isin(CLOSED).sum())
    if "outstandingPlantPayables" in fin:
        sm = fin[fin.labelId.isin(small) & (fin.week % 13 == 0)]
        r["Small plant payables, median (owing labels)"] = float(sm.loc[sm.outstandingPlantPayables > 0, "outstandingPlantPayables"].median()) \
            if (sm.outstandingPlantPayables > 0).any() else 0.0

    # Distribution access, per year
    led = pd.read_csv(prefix + "-deal-ledger.csv")
    sig = led[led.resolution == "Signed"]
    sig = sig.assign(year=year_of(sig.eventWeek))
    ide = pd.read_csv(prefix + "-independent-distribution-events.csv")
    ide = ide.assign(year=year_of(ide.week))
    for y in YEARS:
        g = fin[fin.year == y]
        alive = set(g[g.labelId.isin(small) & ~g.status.isin(CLOSED + ["Acquired"])].labelId)
        s = sig[(sig.year == y) & sig.clientId.isin(small)]
        r[f"Small signing P&D {y} (% of alive)"] = 100 * s.clientId.nunique() / max(1, len(alive))
        r[f"P&D deals, all clients {y}"] = int((sig.year == y).sum())
        si = ide[(ide.year == y) & ide.labelId.isin(small)]
        r[f"wholesale placements Small {y} (direct/hop)"] = "%d (%d/%d)" % (len(si), si.provenInRegion.sum(), (~si.provenInRegion).sum())

    # Small in-run singles: units, coverage, home share, by release year
    fmt = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=["recordId", "format"]).drop_duplicates("recordId")
    singles = set(fmt.loc[fmt.format == "Single", "recordId"])
    rec = pd.read_csv(prefix + "-records.csv", usecols=["week", "recordId", "labelId", "weeksSinceRelease", "currentPosition",
                                                       "totalUnitsSold", "coveredRegionCount"])
    firstSeen = rec.sort_values("week").groupby("recordId").first()
    new = firstSeen[(firstSeen.week > 1) & (firstSeen.weeksSinceRelease <= 1) & firstSeen.index.isin(singles)
                    & firstSeen.labelId.isin(small)]
    relYear = pd.Series(year_of(new.week), index=new.index)
    sr = rec[rec.recordId.isin(new.index)]
    units = sr.groupby("recordId").totalUnitsSold.max()
    wk6 = sr[sr.weeksSinceRelease == 6].set_index("recordId").coveredRegionCount
    charted = sr.groupby("recordId").currentPosition.max() > 0
    geo = pd.read_csv(prefix + "-label-geography.csv").set_index("labelId").homeRegion
    reg = pd.read_csv(prefix + "-completed-week-settlement-regional.csv", usecols=["recordId", "regionId", "finalCleared"])
    reg = reg[reg.recordId.isin(new.index)]
    reg["home"] = reg.recordId.map(new.labelId).map(geo)
    reg["year"] = reg.recordId.map(relYear)
    for y in YEARS:
        ids = relYear[relYear == y].index
        u = units.reindex(ids).dropna()
        r[f"Small singles {y}: n, median / p90 units"] = "%d, %.0f / %.0f" % (len(u), u.median(), u.quantile(.9)) if len(u) else "0"
        v = wk6.reindex(ids).dropna()
        r[f"  covered regions wk6 {y}"] = float(v.median()) if len(v) else float("nan")
        g = reg[reg.year == y]
        tot = g.finalCleared.sum()
        r[f"  copies outside home {y} (%)"] = 100 * g.loc[g.regionId != g.home, "finalCleared"].sum() / tot if tot else float("nan")
        r[f"  ever charted {y} (%)"] = 100 * charted.reindex(ids).fillna(False).mean() if len(ids) else float("nan")

    outcomes = pd.read_csv(prefix + "-release-outcomes.csv")
    so = outcomes[outcomes.labelId.isin(small) & (outcomes.format == "Single")]
    r["starting-Small retired singles losing money (%)"] = 100 * so.realizedNet.le(0).mean()
    r["  median realized net per single"] = float(so.realizedNet.median())

    settle = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=["year", "labelTier", "format", "totalUnits"])
    for y in YEARS:
        g = settle[settle.year == y]
        tot = g.totalUnits.sum()
        r[f"units {y} (M) / album % / Small tier %"] = "%.2f / %.1f / %.1f" % (
            tot / 1e6, 100 * g.loc[g.format == "Album", "totalUnits"].sum() / tot, 100 * g.loc[g.labelTier == "Small", "totalUnits"].sum() / tot)

    conc = pd.read_csv(prefix + "-concentration.csv").set_index("year")
    for y in YEARS:
        if y not in conc.index: continue
        c = conc.loc[y]
        r[f"chart {y}: MajorFam % / C4 % / entries / firms"] = "%.1f / %.1f / %d / %d" % (
            100 * c.majorFamilyChartShare, 100 * c.c4ChartShare, c.chartEntries, c.firmsCharting)
        r[f"  Small chart entries {y}"] = int(c.chartEntriesSmall)

    out = subprocess.run([sys.executable, "-I", os.path.join(HERE, "analyze_regional_sync.py"), prefix], capture_output=True, text=True)
    if out.returncode == 0:
        s = json.loads(out.stdout).get("settlement", {})
        r["top11 overlap mean / median"] = "%.3f / %.3f" % (s.get("top11Overlap", np.nan), s.get("top11OverlapMedian", np.nan))
        bp = s.get("top11OverlapByPair", {})
        if bp:
            v = np.array(list(bp.values()))
            r["top11 by-pair min / max"] = "%.3f / %.3f" % (v.min(), v.max())
    else:
        r["top11 overlap"] = "error"
    return r


def fmt(v):
    return ("%.1f" % v) if isinstance(v, float) else str(v)


if __name__ == "__main__":
    prefixes = sys.argv[1:]
    rows = {p: arm(p) for p in prefixes}
    names = [os.path.basename(p) for p in prefixes]
    keys = []
    for r in rows.values():
        keys += [k for k in r if k not in keys]
    w = max(len(k) for k in keys) + 2
    print("".ljust(w) + "".join(n.rjust(24) for n in names))
    for k in keys:
        print(k.ljust(w) + "".join(fmt(rows[p].get(k, "")).rjust(24) for p in prefixes))
