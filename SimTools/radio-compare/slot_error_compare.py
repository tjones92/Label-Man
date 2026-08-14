import pandas as pd, glob, os, sys
pd.set_option('display.width', 250)
pd.set_option('display.max_rows', 400)
L = r"C:\Project\Label-Man\SimLogs"

BENCH = {
 "Soul":            [ 6,  9,  7, 15, 14, 22, 22, 28, 28, 28],
 "TraditionalPop":  [22, 16, 15, 20, 14, 11,  8,  9, 12,  7],
 "TeenPop":         [28, 18, 20,  8,  5,  7,  6,  9,  2,  2],
 "RnB":             [12, 18, 28, 21, 10,  2,  3,  0,  0,  0],
 "RockAndRoll":     [14, 12, 12, 11,  8,  3,  6,  6,  5,  2],
 "Country":         [ 9,  7,  7,  8,  2,  1,  2,  2,  1,  3],
 "DooWop":          [ 7, 10,  6,  3,  0,  0,  0,  1,  0,  0],
 "EasyListening":   [ 5,  8,  8,  5,  3,  4,  5,  0,  6,  8],
 "SurfRock":        [ 3,  2,  1,  6,  9,  2,  3,  0,  0,  1],
 "Comedy":          [ 3,  3,  2,  3,  1,  0,  0,  2,  0,  1],
 "Folk":            [ 1,  1,  3,  7,  3,  2,  1,  2,  2,  0],
 "BritishBeat":     [ 0,  0,  0,  0, 24, 15,  8,  3,  3,  0],
 "BritishPop":      [ 0,  0,  0,  0,  2, 12,  6,  7,  3,  4],
 "FolkRock":        [ 0,  0,  0,  0,  0, 12, 14,  6,  5,  3],
 "GarageRock":      [ 0,  0,  0,  0,  3,  5, 12,  4,  1,  1],
 "BritishBlues":    [ 0,  0,  0,  0,  0,  5,  3,  2,  0,  0],
 "SunshinePop":     [ 0,  0,  0,  0,  0,  0,  4, 10,  4, 12],
 "PsychedelicRock": [ 0,  0,  0,  0,  0,  0,  1,  9, 10,  6],
 "Bubblegum":       [ 0,  0,  0,  0,  0,  0,  0,  0,  7,  9],
 "Funk":            [ 0,  0,  0,  0,  0,  1,  0,  1,  4,  6],
 "HardRock":        [ 0,  0,  0,  0,  0,  0,  0,  1,  5,  3],
 "Jazz":            [ 0,  1,  1,  0,  1,  1,  0,  0,  1,  2],
 "CountryRock":     [ 0,  0,  0,  0,  0,  0,  0,  0,  0,  6],
}
YEARS = list(range(1960,1970))
bench = pd.DataFrame(BENCH, index=YEARS).T

def load(run):
    f = os.path.join(L, f"{run}-year-end-hot100.csv")
    d = pd.read_csv(f)
    return d.pivot_table(index="genre", columns="year", values="yearEndSlots", aggfunc="sum").fillna(0).astype(int)

runs = sys.argv[1:]
tabs = {r: load(r) for r in runs}

allg = sorted(set(bench.index) | set().union(*[set(t.index) for t in tabs.values()]))
rows = []
for g in allg:
    b = bench.loc[g] if g in bench.index else pd.Series(0, index=YEARS)
    rec = {"genre": g, "bench": int(b.sum())}
    for r in runs:
        t = tabs[r]
        v = t.loc[g].reindex(YEARS).fillna(0) if g in t.index else pd.Series(0, index=YEARS)
        rec[r] = int(v.sum())
        rec[r+"_err"] = int((v - b).abs().sum())
    rows.append(rec)
df = pd.DataFrame(rows).set_index("genre")
print(df.to_string())
print()
print("TOTAL ABS SLOT ERROR (decade, all genres):")
for r in runs:
    print(f"  {r:38s} {df[r+'_err'].sum():5d}")
