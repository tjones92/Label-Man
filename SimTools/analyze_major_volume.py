"""Big-5 single volume screen (LpGenreAllocationFollowup.md §57). Usage from the repo root:
python SimTools/analyze_major_volume.py RUN [RUN ...]      (RUN = e.g. zfg-2002-sc; reads SimLogs/RUN-*.csv)
Per run and year (1960, 1961): singles issued by tier and the Big-5 share of issues (history ~1,080 of ~6,000, ~18%);
Big-5 single slots issued / cancelled; launches, Hot 100 entry and Top 10 per launch for Big-5 newcomers (NewSigning or
Unsigned at launch) and established acts (launches to week 92 only, so 1961 records have time to climb); Big-5 newcomer
Top 10 records (history ~4.5 a year, 1961-64); Big-5 Top 10 slot share (history 34.2 / 20.6) and CR4 (34.6 / 27.9);
cleared single units per single issued and the mean rationing factor; labels defunct (cumulative)."""
import os
import sys
import pandas as pd


def csv(path):
    """A run's telemetry: NAME.csv, else NAME.csv.gz (--gzip-telemetry)."""
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"

B5 = {f"label_000{i}" for i in range(1, 6)}
NEW = {"NewSigning", "Unsigned"}


def one(run):
    p = f"SimLogs/{run}-"
    o = {}
    q = pd.read_csv(csv(p + "single-launch-quality.csv"), usecols=["year", "singleId", "labelId", "tier"])
    q.loc[q.labelId.isin(B5), "tier"] = "Big5"
    q.loc[q.tier.isin(["Small", "Boutique"]), "tier"] = "Small/Bout"
    a = pd.read_csv(csv(p + "single-launch-attribution.csv"), usecols=["week", "year", "singleId", "labelId", "career", "quality"])
    r = pd.read_csv(csv(p + "records.csv"), usecols=["week", "year", "recordId", "labelId", "currentPosition"])
    c = r[r.currentPosition.between(1, 100)]
    peak = c.groupby("recordId").currentPosition.min()
    a["peak"] = a.singleId.map(peak)
    a["new"] = a.career.isin(NEW)
    s = pd.read_csv(csv(p + "release-slate.csv"), usecols=["week", "slotId", "labelId", "format", "state"])
    s = s[s.labelId.isin(B5) & s.format.eq("Single")].drop_duplicates("slotId", keep="last")
    s["year"] = 1960 + (s.week - 1) // 52
    mc = pd.read_csv(csv(p + "market-clearing-weekly.csv"), usecols=["year", "clearedSingleUnits", "rationingFactor"])
    f = pd.read_csv(csv(p + "label-finance.csv"), usecols=["week", "labelId", "status"])
    dead = f[f.status == "Defunct"].groupby("labelId").week.min()
    for y in (1960, 1961):
        e = q[q.year == y]
        n = len(e)
        for t in ["Big5", "MidTier", "Independent", "Small/Bout"]:
            o[f"{y} singles issued {t}"] = (e.tier == t).sum()
        o[f"{y} singles issued total"] = n
        o[f"{y} Big-5 share of issues %"] = 100 * (e.tier == "Big5").mean()
        z = s[s.year == y]
        o[f"{y} Big-5 slots issued / cancelled"] = f"{(z.state == 'Issued').sum()} / {(z.state == 'Cancelled').sum()}"
        b = a[(a.year == y) & a.labelId.isin(B5) & (a.week <= (52 if y == 1960 else 92))]
        for lab, g in (("newcomer", b[b.new]), ("established", b[~b.new])):
            o[f"{y} Big-5 {lab} launches (to wk {52 if y == 1960 else 92})"] = len(g)
            o[f"{y} Big-5 {lab} Hot 100 entry %"] = 100 * g.peak.notna().mean()
            o[f"{y} Big-5 {lab} Top 10 per launch %"] = 100 * (g.peak <= 10).mean()
            o[f"{y} Big-5 {lab} quality"] = g.quality.mean()
        fn = a[(a.year == y) & ~a.labelId.isin(B5) & a.new & (a.week <= (52 if y == 1960 else 92))]
        o[f"{y} field newcomer launches"] = len(fn)
        o[f"{y} field newcomer Top 10 per launch %"] = 100 * (fn.peak <= 10).mean()
        nb = b[b.new]
        o[f"{y} newcomer Top 10 premium (Big-5 / field)"] = (nb.peak <= 10).mean() / max(1e-9, (fn.peak <= 10).mean())
        o[f"{y} newcomer Hot 100 premium (Big-5 / field)"] = nb.peak.notna().mean() / max(1e-9, fn.peak.notna().mean())
        bn = a[(a.year == y) & a.labelId.isin(B5) & a.new]
        o[f"{y} Big-5 newcomer Top 10 records"] = int((bn.peak <= 10).sum())
        t10 = c[(c.year == y) & (c.currentPosition <= 10)]
        o[f"{y} Big-5 Top 10 slot share %"] = 100 * t10.labelId.isin(B5).mean()
        o[f"{y} CR4 %"] = 100 * t10.labelId.value_counts().head(4).sum() / len(t10)
        o[f"{y} labels with a Hot 100 record"] = c[c.year == y].labelId.nunique()
        m = mc[mc.year == y]
        o[f"{y} cleared single units per single issued"] = m.clearedSingleUnits.sum() / n
        o[f"{y} mean rationing factor"] = m.rationingFactor.mean()
        o[f"{y} labels defunct (cumulative)"] = int((dead <= (y - 1959) * 52).sum())
    # 1960-61 launches to week 92 pooled (the kill-test read, Big5VolumeAndPremiumHandoff.md candidate 3).
    w = a[a.week <= 92]
    for lab, sel in (("newcomer", w.new), ("established", ~w.new)):
        b, f = w[sel & w.labelId.isin(B5)], w[sel & ~w.labelId.isin(B5)]
        o[f"60-61 {lab} Top 10 premium (Big-5 / field)"] = (b.peak <= 10).mean() / max(1e-9, (f.peak <= 10).mean())
        o[f"60-61 {lab} Hot 100 premium (Big-5 / field)"] = b.peak.notna().mean() / max(1e-9, f.peak.notna().mean())
        o[f"60-61 Big-5 {lab} Top 10 per launch %"] = 100 * (b.peak <= 10).mean()
    first = r.drop_duplicates("recordId")[["recordId"]].merge(
        pd.read_csv(csv(p + "records.csv"), usecols=["recordId", "initialLaunchAwareness", "initialLaunchStock"]).drop_duplicates("recordId"))
    bn = w[w.new & w.labelId.isin(B5)].merge(first, left_on="singleId", right_on="recordId")
    o["60-61 Big-5 newcomer launch awareness"] = bn.initialLaunchAwareness.mean()
    o["60-61 Big-5 newcomer launch stock"] = bn.initialLaunchStock.median()
    return o


runs = sys.argv[1:]
res = {run: one(run) for run in runs}
w = max(len(k) for k in res[runs[0]])
print(f'{"":{w}}' + "".join(f"{r:>20}" for r in runs))
for k in res[runs[0]]:
    vals = [res[r].get(k) for r in runs]
    print(f"{k:{w}}" + "".join((f"{v:>20.3f}" if abs(v) < 2 else f"{v:>20.1f}") if isinstance(v, float) else f"{v!s:>20}" for v in vals))
