"""Regional-hit deal gate (SmallLabelAttritionAndChartMethod.md §9): control vs treatment on the §6/§7 guards.

Usage: python SimTools/analyze_deal_gate.py SimLogs/<control> SimLogs/<treatment> [SimLogs/<control2> SimLogs/<treatment2> ...]

Per arm, first 52 weeks, starting-Small cohort = labels that are Small in week 1 of label-finance:
P&D funnel, closures, per-release units of in-run singles, copies sold outside the home region, covered regions,
wholesale placements, chart breadth (concentration), and regional top-11 overlap (analyze_regional_sync.py).
"""
import json, os, subprocess, sys
import numpy as np, pandas as pd

CLOSED = ["Defunct", "Bankrupt"]
HERE = os.path.dirname(os.path.abspath(__file__))


def arm(prefix):
    r = {}
    fin = pd.read_csv(prefix + "-label-finance.csv", usecols=["week", "labelId", "labelTier", "status"])
    fin = fin[fin.week <= 52]
    w1 = fin[fin.week == 1].set_index("labelId")
    small = set(w1.index[w1.labelTier == "Small"])
    tier0 = w1.labelTier
    end = fin.sort_values("week").groupby("labelId").last()
    endStart = end[end.index.isin(w1.index)]
    for t in ("Small", "Boutique", "Independent"):
        e = endStart[endStart.index.map(tier0) == t]
        r[f"{t} closed wk52"] = 100 * e.status.isin(CLOSED).mean()
    r["Small promoted out wk52"] = 100 * ((endStart.index.map(tier0) == "Small") & (endStart.labelTier != "Small")
                                          & ~endStart.status.isin(CLOSED)).sum() / len(small)

    lab = pd.read_csv(prefix + "-label-directory.csv").set_index("labelId")
    led = pd.read_csv(prefix + "-deal-ledger.csv")
    sig = led[(led.resolution == "Signed") & (led.eventWeek <= 52)]
    ssig = sig[sig.clientId.isin(small)]
    r["P&D deals signed, all clients"] = len(sig)
    r["Small labels signing P&D (%)"] = 100 * ssig.clientId.nunique() / len(small)
    r["Small P&D deals"] = len(ssig)
    dt = ssig.distributorId.map(lab.initialTier).value_counts()
    r["  via Major / MidTier / Indep"] = "%d / %d / %d" % (dt.get("Major", 0), dt.get("MidTier", 0), dt.get("Independent", 0))
    r["  pull / push"] = "%d / %d" % ((ssig.origin == "LabelSought").sum(), (ssig.origin == "DistributorCourted").sum())
    att = pd.read_csv(prefix + "-distribution-offer-attempts.csv")
    att = att[(att.week <= 52) & att.clientId.isin(small)]
    r["Small labels with pull evidence (%)"] = 100 * att[att.persistentRegionalEvidence].clientId.nunique() / len(small)
    r["Small evidence-months"] = int(att.persistentRegionalEvidence.sum())
    ide = pd.read_csv(prefix + "-independent-distribution-events.csv")
    ide = ide[ide.week <= 52]
    r["wholesale placements, Small"] = int(ide.labelId.isin(small).sum())
    si = ide[ide.labelId.isin(small)]
    r["  direct / neighbour hop"] = "%d / %d" % (si.provenInRegion.sum(), (~si.provenInRegion).sum())
    r["Small labels with any house (%)"] = 100 * si.labelId.nunique() / len(small)
    r["wholesale placements, all"] = len(ide)

    fmt = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=["recordId", "format"]).drop_duplicates("recordId")
    singles = set(fmt.loc[fmt.format == "Single", "recordId"])
    rec = pd.read_csv(prefix + "-records.csv", usecols=["week", "recordId", "labelId", "weeksSinceRelease", "currentPosition",
                                                       "totalUnitsSold", "coveredRegionCount"])
    rec = rec[rec.week <= 52]
    first = rec.sort_values("week").groupby("recordId").first()
    # in-run releases: first seen at release age <= 1 after week 1
    new = first[(first.week > 1) & (first.weeksSinceRelease <= 1) & first.index.isin(singles) & first.labelId.isin(small)].index
    sr = rec[rec.recordId.isin(new)]
    units = sr.groupby("recordId").totalUnitsSold.max()
    r["Small in-run singles"] = len(units)
    r["  units p10 / median / p90"] = "%.0f / %.0f / %.0f" % tuple(units.quantile([.1, .5, .9]))
    r["  ever charted (%)"] = 100 * (sr.groupby("recordId").currentPosition.max() > 0).mean()
    wk6 = sr[sr.weeksSinceRelease == 6].set_index("recordId").coveredRegionCount
    relWeek = first.loc[new, "week"]
    for q, (a, b) in {"Q1": (1, 13), "Q2": (14, 26), "Q3": (27, 39), "Q4": (40, 52)}.items():
        ids = relWeek[(relWeek >= a) & (relWeek <= b)].index
        v = wk6.reindex(ids).dropna()
        r[f"  covered regions at wk6, {q} releases"] = float(v.median()) if len(v) else float("nan")

    geo = pd.read_csv(prefix + "-label-geography.csv").set_index("labelId").homeRegion
    reg = pd.read_csv(prefix + "-completed-week-settlement-regional.csv", usecols=["week", "recordId", "regionId", "finalCleared"])
    reg = reg[(reg.week <= 52) & reg.recordId.isin(new)]
    reg["home"] = reg.recordId.map(sr.drop_duplicates("recordId").set_index("recordId").labelId).map(geo)
    tot = reg.finalCleared.sum()
    r["  copies outside home region (%)"] = 100 * reg.loc[reg.regionId != reg.home, "finalCleared"].sum() / tot if tot else float("nan")

    conc = pd.read_csv(prefix + "-concentration.csv")
    c = conc[conc.year == 1960].iloc[0]
    for col in ("firmsCharting", "smallFirmsCharting", "boutiqueFirmsCharting", "independentFirmsCharting", "chartEntries",
                "chartEntriesSmall", "chartEntriesBoutique", "chartEntriesIndependent", "chartEntriesMidTier", "chartEntriesMajor",
                "majorFamilyChartShare", "indieFamilyChartShare", "c4ChartShare"):
        r["chart " + col] = float(c[col]) * (100 if "Share" in col else 1)

    out = subprocess.run([sys.executable, "-I", os.path.join(HERE, "analyze_regional_sync.py"), prefix],
                         capture_output=True, text=True)
    if out.returncode == 0:
        s = json.loads(out.stdout).get("settlement", {})
        r["top11 overlap mean / median"] = "%.3f / %.3f" % (s.get("top11Overlap", np.nan), s.get("top11OverlapMedian", np.nan))
        bp = s.get("top11OverlapByPair", {})
        if bp:
            v = np.array(list(bp.values()))
            r["top11 by-pair min / max"] = "%.3f / %.3f" % (v.min(), v.max())
            for k in sorted(bp):
                if ("greatlakes" in k and ("westcoast" in k or "greatplains" in k)) or ("westcoast" in k and "greatplains" in k):
                    r["  top11 " + k] = "%.3f" % bp[k]
    else:
        r["top11 overlap"] = "error: " + out.stderr.strip().splitlines()[-1] if out.stderr.strip() else "error"
    return r


def fmt(v):
    return ("%.1f" % v) if isinstance(v, float) else str(v)


prefixes = sys.argv[1:]
rows = {p: arm(p) for p in prefixes}
names = [p.split("/")[-1] for p in prefixes]
w = max(len(k) for r in rows.values() for k in r) + 2
print("".ljust(w) + "".join(n.rjust(22) for n in names))
allKeys = []
for r in rows.values():
    for k in r:
        if k not in allKeys: allKeys.append(k)
for k in allKeys:
    print(k.ljust(w) + "".join(fmt(rows[p].get(k, "")).rjust(22) for p in prefixes))
