"""usage: score_arms.py name=fmt ...   fmt has {s} for the seed, e.g. ctl=fund-v1-{s}-gc_of"""
import sys, os
sys.path.insert(0, 'SimTools')
import numpy as np, pandas as pd
import analyze_big5_share_ladder as lad
SEEDS = [2002, 1001, 3003, 6006]
pd.set_option("display.width", 250)
rows = []
for spec in sys.argv[1:]:
    name, fmt = spec.split("=", 1)
    per = []
    for s in SEEDS:
        run = fmt.format(s=s)
        if not os.path.exists(f"SimLogs/{run}-records.csv"): continue
        sc = lad.score(run)[1961]; sc["seed"] = s; per.append(sc)
    d = pd.DataFrame(per)
    if d.empty: continue
    small = d.SmaT10 + d.BouT10
    rows.append({"arm": name, "T10": d.T10.mean(), "T40": d.T40.mean(), "H100": d.H100.mean(), "launch%": d["launch%"].mean(),
                 "prem": (d.T10 / d["launch%"]).mean(), "Mid": d.MidT10.mean(), "Ind": d.IndT10.mean(), "Small": small.mean(),
                 "fieldH100%": d["fieldH100%"].mean(), "b5H100%": d["b5H100%"].mean(), "unitsM": d.unitsM.mean(),
                 "T10 seeds": " / ".join(f"{x:.1f}" for x in d.T10), "T40 seeds": " / ".join(f"{x:.1f}" for x in d.T40), "H100 seeds": " / ".join(f"{x:.1f}" for x in d.H100)})
print(pd.DataFrame(rows).set_index("arm").round(2).to_string())
