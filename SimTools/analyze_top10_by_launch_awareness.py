"""usage: awband.py fmt  -- share of Top 10 weeks by launch-awareness band x tier (singles launched wk 1-92)"""
import sys, os
sys.path.insert(0, 'SimTools')
import numpy as np, pandas as pd
import analyze_realization_root_cause as rc
pd.set_option("display.width", 250)
ORDER = rc.ORDER
fr = pd.concat([rc.frame(sys.argv[1].format(s=s)).assign(seed=s) for s in [2002, 1001, 3003, 6006]])
fr["awb"] = pd.cut(fr.initialAwareness, [0, .25, .5, .7, 1.01], labels=["<.25", ".25-.5", ".5-.7", ".7+"])
n = fr.pivot_table(index="awb", columns="tier", values="t10w", aggfunc="size", observed=True).reindex(columns=ORDER)
t = fr.pivot_table(index="awb", columns="tier", values="t10w", aggfunc="sum", observed=True).reindex(columns=ORDER)
print("singles per seed"); print((n/4).round(0).to_string())
print("share of all Top 10 weeks %"); print((100*t/t.sum().sum()).round(1).assign(all=(100*t.sum(axis=1)/t.sum().sum()).round(1)).to_string())
print("T10 weeks per single"); print((t/n).round(2).to_string())
print("T40 %"); print((100*fr.pivot_table(index="awb", columns="tier", values="t40", aggfunc="mean", observed=True)).reindex(columns=ORDER).round(1).to_string())
