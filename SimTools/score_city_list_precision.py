"""RegionalSyncDesign.md §9x. Per city (LA, Minneapolis Top40 + FullService, weeks 5-44 of 1960): new records listed per week and the
share of them that never reach the national top 100 in 1960. History (analyze_city_list_vs_hot100_history.py, crude title match):
LA 15.8 a week / ~10% never, Minneapolis 10.8 / ~7%.
Usage: score_city_list_precision.py SEEDS TAG..."""
import sys, pandas as pd
L = str(__import__("pathlib").Path(__file__).resolve().parents[1]).replace("\\", "/") + "/SimLogs/"
seeds = [int(x) for x in sys.argv[1].split(",")]
for tag in sys.argv[2:]:
    out = []
    for sd in seeds:
        p = L + f"hs52op{sd}-SMP-HGRPO-{tag}-"
        r = pd.read_csv(p + "records.csv", usecols=["year", "recordId", "currentPosition"])
        ever = set(r[r.year.eq(1960) & r.currentPosition.between(1, 100)].recordId)
        x = pd.read_csv(p + "station-playlists.csv", usecols=["week", "year", "city", "format", "recordId"])
        x = x[x.year.eq(1960) & x.format.isin(["Top40", "FullService"]) & x.city.isin(["Los Angeles", "Minneapolis"]) & x.week.between(5, 44)]
        f = x.groupby(["city", "recordId"]).week.min().reset_index(); f["ever"] = f.recordId.isin(ever); out.append(f)
    d = pd.concat(out)
    g = d.groupby("city").agg(newPerWk=("ever", lambda v: len(v) / 40 / len(seeds)), neverPct=("ever", lambda v: 100 * (1 - v.mean())))
    print(tag, " | ".join(f"{c}: {row.newPerWk:.1f}/wk never {row.neverPct:.1f}%" for c, row in g.iterrows()))
