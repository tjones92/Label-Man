import pandas as pd, os
pd.set_option('display.width', 260); pd.set_option('display.max_rows', 500)
T = r"C:\Project\Label-Man\SimTools\AdjustedHistoricalGenreShareTargets.csv"
t = pd.read_csv(T)
YEARS = [str(y) for y in range(1960, 1970)]
t["key"] = t["genre"].str.replace(" ", "").str.replace("&", "And").str.replace("-", "")
t = t.set_index("key")

BENCH = {
 "Soul":[6,9,7,15,14,22,22,28,28,28], "TraditionalPop":[22,16,15,20,14,11,8,9,12,7],
 "TeenPop":[28,18,20,8,5,7,6,9,2,2], "RnB":[12,18,28,21,10,2,3,0,0,0],
 "RockAndRoll":[14,12,12,11,8,3,6,6,5,2], "Country":[9,7,7,8,2,1,2,2,1,3],
 "DooWop":[7,10,6,3,0,0,0,1,0,0], "EasyListening":[5,8,8,5,3,4,5,0,6,8],
 "SurfRock":[3,2,1,6,9,2,3,0,0,1], "Comedy":[3,3,2,3,1,0,0,2,0,1],
 "Folk":[1,1,3,7,3,2,1,2,2,0], "BritishBeat":[0,0,0,0,24,15,8,3,3,0],
 "BritishPop":[0,0,0,0,2,12,6,7,3,4], "FolkRock":[0,0,0,0,0,12,14,6,5,3],
 "GarageRock":[0,0,0,0,3,5,12,4,1,1], "BritishBlues":[0,0,0,0,0,5,3,2,0,0],
 "SunshinePop":[0,0,0,0,0,0,4,10,4,12], "PsychedelicRock":[0,0,0,0,0,0,1,9,10,6],
 "Bubblegum":[0,0,0,0,0,0,0,0,7,9], "Funk":[0,0,0,0,0,1,0,1,4,6],
 "HardRock":[0,0,0,0,0,0,0,1,5,3], "Jazz":[0,1,1,0,1,1,0,0,1,2],
 "CountryRock":[0,0,0,0,0,0,0,0,0,6],
}
rows = []
for g, slots in BENCH.items():
    if g not in t.index:
        print("MISSING market target row:", g); continue
    mk = t.loc[g, YEARS].astype(float).values
    for i, y in enumerate(range(1960, 1970)):
        if slots[i] == 0 and mk[i] == 0: continue
        rows.append(dict(genre=g, year=y, benchSlots=slots[i], mktTarget=mk[i],
                         ratio=(slots[i]/mk[i]) if mk[i] > 0 else float("inf")))
d = pd.DataFrame(rows)
print("=== HAND-COUNT CHART SLOTS vs HISTORICAL MARKET-SHARE TARGET, ratio = slots% / mkt% ===")
print("(ratio 1.0 = charts exactly in proportion to units; the model realises ~0.2-3.2x)")
piv = d.pivot_table(index="genre", columns="year", values="ratio")
print(piv.round(2).to_string())
print()
print("=== the 15 most chart-efficient genre-years the benchmark pair demands ===")
print(d.sort_values("ratio", ascending=False).head(15).to_string(index=False))
print()
print("=== SunshinePop rows ===")
print(d[d.genre == "SunshinePop"].to_string(index=False))
print()
print("notes:", t.loc["SunshinePop", "notes"])
print("EL notes:", t.loc["EasyListening", "notes"])
print("Bubblegum notes:", t.loc["Bubblegum", "notes"])
