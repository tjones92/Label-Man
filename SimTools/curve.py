import sys, pandas as pd
B = r"C:\Project\Label-Man\SimLogs"
print(f"{'run':<32}{'rank':>6}{'medUnits':>11}{'%of#1':>8}{'medPts':>11}{'%of#1':>8}   Hesbacher")
HES = {1:100.0, 10:52.3, 25:27.4, 50:13.6, 75:7.9, 100:4.8}
for run in sys.argv[1:]:
    d = pd.read_csv(rf"{B}\{run}-records.csv", usecols=["currentPosition","unitsThisWeek","chartPoints"],
                    low_memory=False)
    d = d[(d.currentPosition >= 1) & (d.currentPosition <= 100)]
    base_u = d[d.currentPosition == 1].unitsThisWeek.median()
    base_p = d[d.currentPosition == 1].chartPoints.median()
    for r in [1, 10, 25, 50, 75, 100]:
        s = d[d.currentPosition == r]
        print(f"{run if r==1 else '':<32}{r:>6}{s.unitsThisWeek.median():>11,.0f}"
              f"{100*s.unitsThisWeek.median()/base_u:>8.1f}{s.chartPoints.median():>11,.0f}"
              f"{100*s.chartPoints.median()/base_p:>8.1f}   {HES[r]:>6.1f}")
