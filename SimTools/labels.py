import sys, pandas as pd
BASE = r"C:\Project\Label-Man\SimLogs"
BANDS = "bands: breadth 400-600, MidTier 25-40, owner-Major 45-52"
print(f"{'run':<32}{'breadth':>8}{'MidTr69':>8}{'Major69':>8}{'ownMaj68':>10}{'ownMaj69':>10}{'entr69':>8}")
for run in sys.argv[1:]:
    c = pd.read_csv(rf"{BASE}\{run}-concentration.csv")
    c = c[c.year != 1970]
    last = c.iloc[-1]
    y68 = c[c.year == 1968].iloc[0]; y69 = c[c.year == 1969].iloc[0]
    print(f"{run:<32}{last.cumulativeExactLabelNamesCharting:>8}{last.midTierFirmsCharting:>8}"
          f"{last.majorFirmsCharting:>8}{100*y68.ownerMajorEntries/y68.chartEntries:>10.1f}"
          f"{100*y69.ownerMajorEntries/y69.chartEntries:>10.1f}{last.chartEntries:>8}")
print(BANDS)
for run in sys.argv[1:]:
    c = pd.read_csv(rf"{BASE}\{run}-concentration.csv")
    c = c[c.year != 1970]
    print(f"\n{run}")
    print(f"{'year':>6}{'entries':>9}{'indep':>8}{'mid':>7}{'major':>7}{'indepFirms':>12}{'midFirms':>10}")
    for _, r in c.iterrows():
        print(f"{int(r.year):>6}{r.chartEntries:>9}{r.chartEntriesIndependent:>8}{r.chartEntriesMidTier:>7}"
              f"{r.chartEntriesMajor:>7}{r.independentFirmsCharting:>12}{r.midTierFirmsCharting:>10}")
