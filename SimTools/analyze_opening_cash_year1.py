"""Year-1 comparison across runs: opening cash, closures by starting tier, Small survivors' cash, deals."""
import sys
import pandas as pd

CLOSED = ["Defunct", "Bankrupt"]
rows = []
for prefix in sys.argv[1:]:
    fin = pd.read_csv(prefix + "-label-finance.csv", usecols=["week", "labelId", "labelTier", "status", "cashReserves"])
    fin = fin[fin.week <= 52]
    w1 = fin[fin.week == 1].set_index("labelId")
    cohort = fin[fin.labelId.isin(w1.index)].copy()
    cohort["tier0"] = cohort.labelId.map(w1.labelTier)
    r = {"run": prefix.split("/")[-1]}
    small = w1[w1.labelTier == "Small"]
    r["small cash0 p10/med/p90"] = "%.0f / %.0f / %.0f" % tuple(small.cashReserves.quantile([.1, .5, .9]))
    for wk in (13, 26, 39, 52):
        snap = cohort[cohort.week <= wk].sort_values("week").groupby("labelId").last()
        r["Small closed wk%d" % wk] = "%.1f%%" % (100 * snap[snap.tier0 == "Small"].status.isin(CLOSED).mean())
    end = cohort.sort_values("week").groupby("labelId").last()
    for t in ("Boutique", "Independent"):
        r["%s closed wk52" % t] = "%.1f%%" % (100 * end[end.tier0 == t].status.isin(CLOSED).mean())
    live = end[(end.tier0 == "Small") & ~end.status.isin(CLOSED + ["Acquired"])]
    r["Small survivor cash wk52 med"] = "%.0f" % live.cashReserves.median()
    led = pd.read_csv(prefix + "-deal-ledger.csv")
    sig = led[(led.resolution == "Signed") & (led.eventWeek <= 52) & led.clientId.isin(small.index)]
    r["Small P&D deals yr1"] = "%d (%.0f%%)" % (sig.clientId.nunique(), 100 * sig.clientId.nunique() / len(small))
    births = fin[~fin.labelId.isin(w1.index)]
    if len(births):
        b0 = births.sort_values("week").groupby("labelId").first()
        bend = births.sort_values("week").groupby("labelId").last()
        r["births / closed by wk52"] = "%d / %d" % (len(b0), bend.status.isin(CLOSED).sum())
        r["birth Small cash0 med"] = "%.0f" % b0[b0.labelTier == "Small"].cashReserves.median()
    rows.append(r)
pd.set_option("display.width", 250)
print(pd.DataFrame(rows).set_index("run").T.to_string())
