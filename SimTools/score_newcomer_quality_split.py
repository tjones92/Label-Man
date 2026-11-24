"""LpGenreAllocationFollowup.md §87. Usage from the repo root: python SimTools/score_newcomer_quality_split.py SEEDS ARM [ARM ...]
NewSigning/Unsigned single launches wk 1-92 (+12): H100 entry, Top 10 per entry and per launch by Big-5 plug outcome, MidTier and
field, and the Big-5 rates against the field at the same quality (five bins)."""
import os, re, sys, contextlib, io
sys.path.insert(0, "SimTools")
import numpy as np, pandas as pd
from analyze_big5_chart_opportunities import simulated, source
PLUG = re.compile(r"PROMO_PLUG week=(\d+) label=(\S+) tier=(\S+) record=(\S+) artist=(\S*) career=(\S*) hit=(\S+) quality=(\S+) outcome=(\S+)")
seeds = [int(s) for s in sys.argv[1].split(",")]
pd.set_option("display.width", 220)
for arm in sys.argv[2:]:
    fr = []
    for s in seeds:
        run = f"{arm}-{s}-sc"
        with contextlib.redirect_stdout(io.StringIO()):
            a = simulated(run)
        q = pd.read_csv(source(run, "single-launch-quality.csv"), usecols=["singleId", "tier"]).set_index("singleId").join(pd.read_csv(source(run, "single-launch-attribution.csv"), usecols=["singleId", "quality"]).set_index("singleId"))
        a = a.join(q)
        plugs = {}
        with open(f"SimLogs/writing-calibration/{run}.log", encoding="utf-8", errors="ignore") as f:
            for line in f:
                m = PLUG.search(line)
                if m: plugs[m.group(4).strip('"')] = m.group(9)
        a["plug"] = a.index.map(plugs).fillna("NotRouted")
        a = a[a.week.between(1, 92) & a.new].copy()
        a["h"] = a.debut.sub(a.week).between(0, 12)
        a["t"] = a.t10.sub(a.week).between(0, 12)
        a["seed"] = s
        fr.append(a)
    a = pd.concat(fr)
    a["grp"] = np.where(a.big, "Big5:" + a.plug, np.where(a.tier.eq("MidTier"), "MidTier", "field"))
    def agg(g):
        return pd.Series({"launch/seed": len(g) / len(seeds), "q": g.quality.mean(), "H100%": 100 * g.h.mean(),
                          "T10/entry%": 100 * g.t.sum() / max(g.h.sum(), 1), "T10%": 100 * g.t.mean(), "T10/seed": g.t.sum() / len(seeds)})
    t = a.groupby("grp").apply(agg, include_groups=False)
    t.loc["Big5:all"] = agg(a[a.big])
    print("==", arm); print(t.round(2).to_string())
    # field rate at Big-5 quality mix (5 bins)
    bins = [0, .66, .70, .74, .78, 1.01]
    a["qb"] = pd.cut(a.quality, bins)
    f = a[~a.big & a.grp.eq("field")]
    for col in ("h", "t"):
        fr_ = f.groupby("qb", observed=True)[col].mean()
        for name, g in (("Big5:all", a[a.big]), ("Big5:Routine", a[a.big & a.plug.eq("Routine")])):
            exp = g.qb.map(fr_).astype(float).mean()
            print(f"  {name} {col}: actual {100*g[col].mean():.2f}% vs field-at-same-quality {100*exp:.2f}% -> {g[col].mean()/exp:.2f}x")
    print(f"  raw premium T10 vs field: {a[a.big].t.mean()/f.t.mean():.2f}x")
