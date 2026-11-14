"""usage: realize.py LW UPSIDE fmt   -- T40 per connected single / per hit draw by tier, 4 seeds; kfd params overridden"""
import sys, os
sys.path.insert(0, 'SimTools')
import numpy as np, pandas as pd
import keyed_fit_draws as kfd
lw, up, fmt = float(sys.argv[1]), float(sys.argv[2]), sys.argv[3]
kfd.LAUNCH_WEIGHT = lw; kfd.UPSIDE = up
kfd.LOG_NORM = kfd._logmean(kfd.KINK); kfd.UNEXPOSED_CAP = max(-kfd.SIGMA * kfd.KINK - kfd.LOG_NORM, np.log(kfd.FLOOR))
import analyze_realization_root_cause as rc
pd.set_option("display.width", 250)
ORDER = rc.ORDER
rows = []; allf = []
for s in [2002, 1001, 3003, 6006]:
    run = fmt.format(s=s)
    a = rc.enrich(run, rc.frame(run)); a["seed"] = s; allf.append(a)
    a["hitref"] = a.z > kfd.KINK
    c = a[a.conn]; h = a[a.hitref]
    row = {"seed": s}
    for t in ORDER:
        row["conn " + t] = 100 * c[c.tier == t].t40.mean(); row["hit " + t] = 100 * h[h.tier == t].t40.mean()
        row["h100hit " + t] = 100 * h[h.tier == t].h100.mean()
    row["B5/Small conn"] = row["conn Big5"] / row["conn Small/Bout"]; row["B5/Ind conn"] = row["conn Big5"] / row["conn Independent"]
    row["B5/Small hit"] = row["hit Big5"] / row["hit Small/Bout"]; row["B5/Ind hit"] = row["hit Big5"] / row["hit Independent"]
    rows.append(row)
d = pd.DataFrame(rows).set_index("seed"); d.loc["mean"] = d.mean()
print(d.round(2).T.to_string())
al = pd.concat(allf)
al["zb"] = pd.cut(al.z, [-9, 0, 0.5, 1.036, 9], labels=["z<0", "0-.5", ".5-kink", ">kink"])
print("T40 % by label-blind z band (pooled)"); print((100*al.pivot_table(index="zb", columns="tier", values="t40", aggfunc="mean", observed=True)).reindex(columns=ORDER).round(1).to_string())
t = al.pivot_table(index="zb", columns="tier", values="t10w", aggfunc="sum", observed=True).reindex(columns=ORDER)
print("share of tier's Top 10 weeks by z band"); print((100*t/t.sum()).round(1).to_string())
print("share of all Top 10 weeks (launch wk 1-92)"); print((100*t.sum()/t.sum().sum()).round(1).to_string())
