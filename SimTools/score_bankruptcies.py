"""Bankruptcies by tier at closure, per arm (mean over seeds). Usage: bankrupt.py SEEDS TAG [TAG ...]"""
import re, sys
import pandas as pd
L = "C:/Project/Label-Man/SimLogs/"
seeds = sys.argv[1].split(",")
rows = []
for tag in sys.argv[2:]:
    for s in seeds:
        pre = f"hs52op{s}-SMP-HGRPO-{tag}"
        try:
            text = open(L + "regprobe/" + pre + "-console.log", encoding="utf-8", errors="replace").read()
            fin = pd.read_csv(L + pre + "-label-finance.csv", usecols=["week", "labelName", "labelTier", "status"])
        except FileNotFoundError:
            continue
        names = set(re.findall(r"\[LabelManager\] (.*) has closed\. Reason: Bankruptcy", text))
        tier = fin[fin.status != "Defunct"].sort_values("week").groupby("labelName").labelTier.last()
        t = pd.Series([tier.get(n, "?") for n in names]).value_counts()
        rows.append(dict(arm=tag, seed=s, total=len(names), **t.to_dict()))
d = pd.DataFrame(rows).fillna(0)
print(d.to_string(index=False))
print(d.drop(columns="seed").groupby("arm", sort=False).mean().round(1).to_string())
