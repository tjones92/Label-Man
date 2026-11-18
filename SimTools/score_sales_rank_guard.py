"""Sales-ranked chart-bottom guard (Big5ChartOpportunityInvestigation.md, "Where the chart bottom's slots go").
Usage from the repo root:  python SimTools/score_sales_rank_guard.py RUN [RUN ...]   (RUN = e.g. srkc-2002-sc; needs a run
made with --sales-rank-telemetry, which writes RUN-sales-rank.csv(.gz))

For 1961 (weeks 53-104), medians over weeks: units of the Nth best-selling single among all singles (the sales chart) and of
the published Hot 100's No. N, and the #10 / #100 ratio on each. The chart-bottom target (findings §12) put sales-ranked
#10 / #100 between about 5x (Haampland, measured sales) and 11x (Carroll / Hesbacher chart-score curves). Also: how many of the
sales chart's top 100 are not on the published chart, and of those how many are 2+ weeks past their sales peak."""
from pathlib import Path
import sys
import pandas as pd

LOG = Path(__file__).resolve().parents[1] / "SimLogs"


def gz(p):
    p = Path(p)
    return p if p.exists() else Path(str(p) + ".gz")


rows = {}
for run in sys.argv[1:]:
    s = pd.read_csv(gz(LOG / f"{run}-sales-rank.csv"))
    s = s[s.week >= 53]
    piv = s.pivot_table(index="week", columns="salesRank", values="units")
    r = pd.read_csv(gz(LOG / f"{run}-records.csv"), usecols=["week", "recordId", "currentPosition", "unitsThisWeek", "weeksSincePeakUnits"])
    c = r[(r.week >= 53) & r.currentPosition.between(1, 100)]
    pub = c.pivot_table(index="week", columns="currentPosition", values="unitsThisWeek")
    o = {}
    for n in (1, 10, 40, 60, 100):
        o[f"sales No. {n} units"] = piv[n].median()
        o[f"published No. {n} units"] = pub[n].median()
    o["sales #10 / #100"] = (piv[10] / piv[100]).median()
    o["published #10 / #100"] = (pub[10] / pub[100]).median()
    o["sales #40 / #100"] = (piv[40] / piv[100]).median()
    o["published #40 / #100"] = (pub[40] / pub[100]).median()
    # sales ranks 60 and 100: listed on the chart, and if not, past their peak
    for n in (60, 100):
        row = s[s.salesRank == n]
        o[f"sales No. {n}: unlisted %"] = 100 * (row.publishedPosition == 0).mean()
        o[f"sales No. {n}: unlisted and 2+ wk past peak %"] = 100 * ((row.publishedPosition == 0) & (row.weeksSincePeakUnits >= 2)).mean()
    rows[run] = o
print(pd.DataFrame(rows).round(2).to_string())
