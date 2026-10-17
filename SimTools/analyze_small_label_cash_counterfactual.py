"""Static (no behavioral response) cash-path counterfactual for the starting-Small cohort.

price_ratio: label's per-unit price as a fraction of the current booked $0.89.
returns: share of shipped copies returned unsold; the label pays pressing on shipped copies.
"""
import sys
import numpy as np
import pandas as pd

prefix = sys.argv[1]
cols = ["week", "year", "labelId", "labelTier", "status", "cashReserves", "weeklyGross", "weeklyCogs",
        "weeklySkim", "weeklyRoyalty", "weeklyNet", "monthlyOverhead", "recentMonthlyCashFlow"]
f = pd.read_csv(prefix + "-label-finance.csv", usecols=cols)
first = f.groupby("labelId", sort=False).first()
ids = first[(first.week == 1) & first.labelTier.eq("Small")].index
f = f[f.labelId.isin(ids)].sort_values(["labelId", "week"])

tot = f.groupby("labelId")[["weeklyGross", "weeklyCogs", "weeklySkim", "weeklyRoyalty", "weeklyNet"]].sum()
print("starting Small:", len(ids))
s = tot.sum()
print("5y cohort totals ($M): gross %.2f cogs %.2f skim %.2f royalty %.2f net %.2f" % tuple(s / 1e6))
print("overhead 5y ($M, monthly overhead x 60 approx): %.2f" % (first.loc[ids].monthlyOverhead.median() * 60 * len(ids) / 1e6))
print("net / gross %.3f   skim / (gross-cogs) %.3f" % (s.weeklyNet / s.weeklyGross, s.weeklySkim / (s.weeklyGross - s.weeklyCogs)))

def run(price_ratio, returns):
    g, c, sk = f.weeklyGross, f.weeklyCogs, f.weeklySkim
    skim_rate = (sk / (g - c).where((g - c) > 0)).fillna(0).clip(0, 1)
    g2 = g * price_ratio
    c2 = c / (1 - returns)
    net_delta = ((g2 - c2) - (g - c)) * (1 - skim_rate)  # royalty stays on list
    cum = net_delta.groupby(f.labelId).cumsum()
    cash2 = f.cashReserves + cum
    alive = ~f.status.isin(["Defunct", "Bankrupt", "Acquired"])
    neg = (cash2 < 0) & alive
    # two consecutive month-end negatives ~ 5+ consecutive negative weeks
    run_len = neg.groupby((~neg).cumsum()).cumsum()
    failed = run_len.ge(5).groupby(f.labelId).any()
    fail_year = f[run_len.ge(5)].groupby("labelId").year.min()
    end = cash2.groupby(f.labelId).last()
    return failed.mean(), fail_year.value_counts().sort_index().to_dict(), np.median(end)

for pr, ret in [(1, 0), (0.63 / 0.89, 0), (0.45 / 0.89, 0), (1, 0.30), (0.63 / 0.89, 0.30), (0.45 / 0.89, 0.30)]:
    rate, by_year, med = run(pr, ret)
    print(f"price {pr*0.89:.2f}  returns {ret:.0%}: static fail {rate:.1%}  by year {by_year}  median end cash ${med:,.0f}")
