"""LpGenreAllocationFollowup.md §88: the opening tier quotas screen. Usage from the repo root:
python SimTools/score_opening_tier_quotas.py SEEDS ARM [ARM ...]
Per arm, mean over seeds: Big-5 Top 10 share 1960 / 1961 and its slot points by career at launch (newcomer / other / prewarm
record); opening star-archetype acts by tier; Big-5 share of the week-1 Hot 100 and the weeks 1-4 Top 10; Big-5 Top 10s from
weeks 1-92 launches by career (+12); Big-5 newcomer Top 10 records by year; Major singles a label 1961; labels inactive at
week 104 (company-monthly)."""
import contextlib, io, os, sys
sys.path.insert(0, "SimTools")
import pandas as pd
from analyze_big5_chart_opportunities import simulated
B5 = {f"label_000{i}" for i in range(1, 6)}
NEW = {"NewSigning", "Unsigned"}
STAR = {"AdultStandardBearer", "CurrentHitmaker", "EstablishedHitmaker"}
def gz(p): return p if os.path.exists(p) else p + ".gz"

def one(arm, s):
    p = f"SimLogs/{arm}-{s}-sc-"
    a = pd.read_csv(gz(p + "single-launch-attribution.csv"), usecols=["singleId", "labelId", "career", "openingArchetype", "artistId", "week", "year"]).set_index("singleId")
    a = a.join(pd.read_csv(gz(p + "single-launch-quality.csv"), usecols=["singleId", "tier"]).set_index("singleId"))
    r = pd.read_csv(gz(p + "records.csv"), usecols=["week", "year", "recordId", "labelId", "artistId", "currentPosition"])
    c = r[r.currentPosition.between(1, 100)].copy()
    c["b5"] = c.labelId.isin(B5)
    o = {}
    t10 = c[c.currentPosition <= 10].copy()
    t10["grp"] = t10.recordId.map(a.career).map(lambda v: "prewarm" if pd.isna(v) else ("new" if v in NEW else "other"))
    for y in (1960, 1961):
        ty = t10[t10.year.eq(y)]
        o[f"B5 T10 share {y}"] = 100 * ty.b5.mean()
        for g in ("new", "other", "prewarm"):
            o[f"  {y} pts {g}"] = 100 * (ty.b5 & ty.grp.eq(g)).sum() / len(ty)
    w1 = c[c.week.eq(1)]
    o["open: B5 % of wk-1 H100"] = 100 * w1.b5.mean()
    o["open: B5 % of wk 1-4 T10"] = 100 * c[c.week.le(4) & c.currentPosition.le(10)].b5.mean()
    st = a[a.openingArchetype.isin(STAR)].sort_values("week").drop_duplicates("artistId")
    o["star acts: Big-5"] = st.labelId.isin(B5).sum()
    o["star acts: MidTier"] = (~st.labelId.isin(B5) & st.tier.eq("MidTier")).sum()
    first = t10.groupby("recordId").year.min()
    nb = first.reindex(a[a.labelId.isin(B5) & a.career.isin(NEW)].index).dropna()
    o["B5 newcomer T10 records 1960"] = (nb == 1960).sum()
    o["B5 newcomer T10 records 1961"] = (nb == 1961).sum()
    with contextlib.redirect_stdout(io.StringIO()):
        x = simulated(f"{arm}-{s}-sc")
    x = x[x.big & x.week.between(1, 92)]
    hit = x.t10.sub(x.week).between(0, 12)
    for k, g in (("NewSigning", ["NewSigning", "Unsigned"]), ("Rising+Established", ["Rising", "Established"]), ("Star+Superstar", ["Star", "Superstar"])):
        o[f"B5 T10 wk1-92 launches: {k}"] = hit[x.career.isin(g)].sum()
    for y in (1960, 1961):
        o[f"Major singles a label {y}"] = a[a.labelId.isin(B5) & a.year.eq(y)].shape[0] / 5
    pr = pd.read_csv(gz(p + "programme-resources.csv"), usecols=["week", "labelId", "staffCoverage"])
    pr = pr[pr.labelId.isin(B5)]
    o["Major staff coverage 1960"] = pr[pr.week <= 52].staffCoverage.mean()
    o["Major staff coverage 1961"] = pr[pr.week > 52].staffCoverage.mean()
    # Big-5 newcomer Top 10 records by year, opening-roster act vs signed in-run (signing event before launch)
    ev = pd.read_csv(gz(p + "artist-population-events.csv"), usecols=["week", "eventType", "artistId"])
    sg = ev[ev.eventType.isin(["signing", "re-signing"])].groupby("artistId").week.min()
    nbx = a[a.labelId.isin(B5) & a.career.isin(NEW)].copy()
    nbx["inrun"] = nbx.artistId.map(sg).le(nbx.week).fillna(False)
    nbx["hy"] = first.reindex(nbx.index).values
    for name, g in (("opening-roster", nbx[~nbx.inrun]), ("in-run", nbx[nbx.inrun])):
        o[f"B5 newcomer T10 {name} 1960"] = (g.hy == 1960).sum()
        o[f"B5 newcomer T10 {name} 1961"] = (g.hy == 1961).sum()
    m = pd.read_csv(gz(p + "company-monthly.csv"), usecols=["week", "labelId", "active"]).sort_values("week").groupby("labelId").last()
    o["labels inactive wk 104"] = (m.active.astype(str) == "False").sum()
    return o

seeds = sys.argv[1].split(",")
rows = {arm: pd.DataFrame([one(arm, s) for s in seeds]).mean() for arm in sys.argv[2:]}
pd.set_option("display.width", 200)
print(pd.DataFrame(rows).round(2).to_string())
print("History: Big-5 T10 share (corporate families, §96) 1960 27 (adopted) / 1961 22.1 ± 4.3 band (pooled 1961-62 24.0 ± 3.2; 4+ seeds); newcomer T10 records ~4.5/yr; majors ~24% of top-20 records 1959-60, Q1 1960 T10 32%; Big-5 ~37% of 1960 2+ T10 acts")
