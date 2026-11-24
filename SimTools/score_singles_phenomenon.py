"""LpGenreAllocationFollowup.md §97: the singles phenomenon screen (--singles-phenomenon, §73 stage C).
Usage from the repo root: python SimTools/score_singles_phenomenon.py SEEDS ARM [ARM ...]

Per arm, mean over seeds:
- phenomena: onsets (SINGLES_PHENOMENON_ONSET in NAME.log), onset week, label tier; for the biggest phenomenon act its best
  weekly share of national singles units, best week (units), most simultaneous Top 10 / Top 5 records, best single.
- the singles market: biggest single lifetime, mean of the top 5, No. 1 records' median lifetime units, best record-week,
  the largest act weekly share, the share of Top 10 slot-weeks held by each year's leading act.
- Top 10 turnover: distinct Top 10 records a year and their mean Top 10 weeks; published #10/#100 units (weekly median).
- LP: biggest album-chart LP (store units) and the phenomenon act's best LP.
Big-5 share, newcomer rows and closures: SimTools/score_opening_tier_quotas.py on the same arms."""
import os, re, sys
import pandas as pd

def gz(p): return p if os.path.exists(p) else p + ".gz"

def one(arm, s):
    run = f"{arm}-{s}-sc"
    p = f"SimLogs/{run}-"
    r = pd.read_csv(gz(p + "records.csv"), usecols=["week", "year", "recordId", "artistId", "labelId", "labelTier",
                                                     "currentPosition", "unitsThisWeek", "totalUnitsSold"])
    m = pd.read_csv(gz(p + "market-clearing-weekly.csv"), usecols=["week", "clearedSingleUnits"]).groupby("week").clearedSingleUnits.sum()
    o = {}
    onsets = []
    log = f"SimLogs/writing-calibration/{run}.log"
    if os.path.exists(log):
        for line in open(log, encoding="utf-8", errors="replace"):
            mo = re.match(r"SINGLES_PHENOMENON_ONSET week=(\d+) artist=(\S+) share=([\d.]+)", line)
            if mo: onsets.append((int(mo.group(1)), mo.group(2), float(mo.group(3))))
    o["phenomena (onsets)"] = len(onsets)
    o["  in 1960 / 1961"] = f"{sum(w <= 52 for w, _, _ in onsets)} / {sum(w > 52 for w, _, _ in onsets)}"
    tier = r.drop_duplicates("artistId", keep="last").set_index("artistId").labelTier
    o["  at Majors"] = sum(tier.get(a) == "Major" for _, a, _ in onsets)
    act_week = r.groupby(["artistId", "week"]).unitsThisWeek.sum().reset_index()
    act_week["share"] = act_week.unitsThisWeek / act_week.week.map(m)
    chart = r[r.currentPosition.between(1, 100)]
    best = None
    if onsets:
        sh = act_week[act_week.artistId.isin([a for _, a, _ in onsets])]
        best = sh.loc[sh.share.idxmax(), "artistId"]
        b = sh[sh.artistId.eq(best)]
        o["top phenomenon: onset week"] = next(w for w, a, _ in onsets if a == best)
        o["top phenomenon: best week share %"] = 100 * b.share.max()
        o["top phenomenon: best week units k"] = b.unitsThisWeek.max() / 1e3
        bc = chart[chart.artistId.eq(best)]
        o["top phenomenon: max Top 10 at once"] = bc[bc.currentPosition <= 10].groupby("week").size().max() if len(bc) else 0
        o["top phenomenon: max Top 5 at once"] = bc[bc.currentPosition <= 5].groupby("week").size().max() if len(bc) else 0
        o["top phenomenon: best single k"] = r[r.artistId.eq(best)].totalUnitsSold.max() / 1e3
        o["top phenomenon: singles in run M"] = act_week[act_week.artistId.eq(best)].unitsThisWeek.sum() / 1e6
    life = r.groupby("recordId").totalUnitsSold.max().sort_values(ascending=False)
    o["biggest single k"] = life.iloc[0] / 1e3
    o["top-5 singles mean k"] = life.head(5).mean() / 1e3
    no1 = r[r.currentPosition.eq(1)].recordId.unique()
    o["No. 1 median lifetime k"] = life.reindex(no1).median() / 1e3
    o["best record-week k"] = r.unitsThisWeek.max() / 1e3
    o["largest act week share %"] = 100 * act_week.share.max()
    t10 = chart[chart.currentPosition <= 10]
    for y in (1960, 1961):
        ty = t10[t10.year.eq(y)]
        o[f"T10 records {y}"] = ty.recordId.nunique()
        o[f"  mean T10 weeks {y}"] = len(ty) / max(1, ty.recordId.nunique())
        o[f"  leading act % of T10 slots {y}"] = 100 * ty.artistId.value_counts().iloc[0] / len(ty)
    pos = chart[chart.currentPosition.isin([10, 100])].pivot_table(index="week", columns="currentPosition", values="unitsThisWeek")
    o["#10/#100 units (median)"] = (pos[10] / pos[100].clip(lower=1)).median()
    try:
        a = pd.read_csv(gz(p + "album-chart.csv"), usecols=["recordId", "artistId", "totalUnitsSold"])
        lp = a.groupby(["recordId", "artistId"]).totalUnitsSold.max().reset_index()
        o["biggest LP k (chart store)"] = lp.totalUnitsSold.max() / 1e3
        if best is not None:
            o["top phenomenon: best LP k"] = lp[lp.artistId.eq(best)].totalUnitsSold.max() / 1e3 if (lp.artistId == best).any() else 0.0
    except FileNotFoundError:
        pass
    return o

seeds = sys.argv[1].split(",")
pd.set_option("display.width", 220)
for arm in sys.argv[2:]:
    rows = [one(arm, s) for s in seeds]
    df = pd.DataFrame(rows, index=seeds).T
    num = df.apply(pd.to_numeric, errors="coerce")
    df["mean"] = num.mean(axis=1).round(2).where(num.notna().all(axis=1), "")
    print(f"== {arm}")
    print(df.to_string())
