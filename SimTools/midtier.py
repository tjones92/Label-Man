"""Reconstruct GetRecentChartingRecordCount for Independents, as section 12.4j did.

Distinct records released within 52 weeks that charted at least once. The promotion bar is 5.
"""
import sys, pandas as pd
B = r"C:\Project\Label-Man\SimLogs"
BAR = 5
TIER = "MidTier"
for run in sys.argv[1:]:
    d = pd.read_csv(rf"{B}\{run}-records.csv",
                    usecols=["year","labelId","labelTier","recordId","currentPosition","weeksSinceRelease"],
                    low_memory=False)
    d = d[(d.currentPosition >= 1) & (d.weeksSinceRelease <= 52) & (d.labelTier == TIER)]
    print(f"\n{run}   ({TIER} labels, distinct records charting within 52wk of release)")
    print(f"{'year':>6}{'labels':>8}{'median':>8}{'p90':>6}{'max':>6}"
          f"{'>=4':>6}{'>=5':>6}{'>=6':>6}{'>=7':>6}{'>=8':>6}")
    for y in sorted(x for x in d.year.unique() if x != 1970):
        s = d[d.year == y].groupby("labelId").recordId.nunique()
        print(f"{y:>6}{len(s):>8}{s.median():>8.0f}{s.quantile(.9):>6.0f}{s.max():>6}"
              f"{(s>=4).sum():>6}{(s>=BAR).sum():>6}{(s>=6).sum():>6}{(s>=7).sum():>6}{(s>=8).sum():>6}")
