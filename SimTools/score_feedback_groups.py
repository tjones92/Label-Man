"""LpGenreAllocationFollowup.md §103-104: where local promotion's Big-5 1960 cost reads back. Usage from the repo root:
python SimTools/score_feedback_groups.py SEEDS ARM [ARM ...]   (ARM like lpmFm52; lpmo / stB resolve per seed)
1960 (weeks 1-52), per seed then mean: Big-5 Top 10 record-week share, small-label Top 10 share, Major (initial tier)
and Big-5 share of single units (label-sales-weekly). Prints each arm's mean and the per-seed difference against `lpmo`
and `stB` (mean ± SE). `lpmo` = lpmo104 on seeds 1001-4004, lpmo52 after; `stB` = stB / stB52 likewise."""
import os, sys
import pandas as pd
B5 = {f"label_000{i}" for i in range(1, 6)}
SMALL = {"Independent", "Small", "Boutique"}
def gz(p): return p if os.path.exists(p) else p + ".gz"
def resolve(arm, s):
    if arm in ("lpmo", "stB"): return (arm + ("104" if arm == "lpmo" else "")) if int(s) <= 4004 else arm + "52"
    return arm

def one(arm, s):
    p = f"SimLogs/{resolve(arm, s)}-{s}-sc-"
    tiers = pd.read_csv(gz(p + "label-directory.csv"), usecols=["labelId", "initialTier"]).set_index("labelId").initialTier
    r = pd.read_csv(gz(p + "records.csv"), usecols=["week", "labelId", "currentPosition"])
    t = r[r.week.between(1, 52) & r.currentPosition.between(1, 10)]
    u = pd.read_csv(gz(p + "label-sales-weekly.csv"), usecols=["week", "labelId", "format", "units"])
    u = u[u.week.between(1, 52) & u.format.eq("Single")]
    tot = u.units.sum()
    return {"B5 T10": 100 * t.labelId.isin(B5).mean(), "small T10": 100 * t.labelId.map(tiers).isin(SMALL).mean(),
            "Major units": 100 * u[u.labelId.map(tiers).eq("Major")].units.sum() / tot,
            "B5 units": 100 * u[u.labelId.isin(B5)].units.sum() / tot}

seeds = sys.argv[1].split(",")
arms = sys.argv[2:]
base = {c: pd.DataFrame([one(c, s) for s in seeds], index=seeds) for c in ("stB", "lpmo")}
data = {a: (base[a] if a in base else pd.DataFrame([one(a, s) for s in seeds], index=seeds)) for a in arms}
pd.set_option("display.width", 200)
for a, d in data.items():
    print(f"\n## {a} ({len(seeds)} seeds)")
    out = pd.DataFrame({"mean": d.mean()})
    for c in ("stB", "lpmo"):
        diff = d - base[c]
        out[f"vs {c}"] = [f"{diff[k].mean():+.2f} ± {diff[k].std(ddof=1) / len(seeds) ** .5:.2f}" if len(seeds) > 1 else f"{diff[k].mean():+.2f}" for k in d.columns]
    print(out.round(2).to_string())
    print("per seed:", d.round(1).to_dict("index"))
