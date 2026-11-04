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
            fin = pd.read_csv(L + pre + "-label-finance.csv")
        except FileNotFoundError:
            continue
        closures = re.findall(r"\[LabelManager\] (.*) has closed\. Reason: Bankruptcy", text)
        live = fin[fin.status != "Defunct"].sort_values("week")
        if "exitReason" in fin:
            # Key by labelId: names repeat (an Independent "Decca Records" beside the Major label_0004).
            ids = set(fin.labelId[fin.exitReason.eq("Bankruptcy")])
            tier = live.groupby("labelId").labelTier.last()
            t = pd.Series([tier.get(i, "?") for i in ids]).value_counts()
            assert len(ids) == len(closures), (pre, len(ids), len(closures))
        else:  # older logs: name lookup, ambiguous when names repeat
            tier = live.groupby("labelName").labelTier.last()
            t = pd.Series([tier.get(n, "?") for n in closures]).value_counts()
        rows.append(dict(arm=tag, seed=s, total=len(closures), **t.to_dict()))
d = pd.DataFrame(rows).fillna(0)
print(d.to_string(index=False))
print(d.drop(columns="seed").groupby("arm", sort=False).mean().round(1).to_string())
