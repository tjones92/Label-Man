import pandas as pd, os, math
pd.set_option('display.width', 260); pd.set_option('display.max_rows', 600)
L = r"C:\Project\Label-Man\SimLogs"

def eff(run):
    ye = pd.read_csv(os.path.join(L, f"{run}-year-end-hot100.csv"))
    sh = pd.read_csv(os.path.join(L, f"{run}-genre-decade-shape.csv"))
    sh = sh[sh.year.between(1960, 1969)]
    sh["mkt%"] = 100*sh["marketUnits"]/sh.groupby("year")["marketUnits"].transform("sum")
    m = sh.merge(ye[["year","genre","yearEndSlots"]], on=["year","genre"], how="left")
    m["yearEndSlots"] = m["yearEndSlots"].fillna(0)
    m["eff"] = m["yearEndSlots"]/m["mkt%"]
    return m

m = eff("radio-chartguard-1001")
big = m[(m["mkt%"] >= 0.5)]
print("=== REALISED chart efficiency (yearEndSlots per 1% market-unit share), chartguard decade run ===")
print("    restricted to genre-years with >=0.5% market share\n")
print("TOP 15 most efficient genre-years the MODEL actually produces:")
print(big.sort_values("eff", ascending=False)[["year","genre","mkt%","yearEndSlots","eff"]].head(15).round(2).to_string(index=False))
print("\nSunshinePop, every year:")
print(m[m.genre == "SunshinePop"][["year","genre","baseline","mkt%","uniqueChartingRecords","top40RecordWeeks","yearEndSlots","eff"]].round(2).to_string(index=False))
print("\nBubblegum (the model's efficiency champion), every year:")
print(m[m.genre == "Bubblegum"][["year","genre","baseline","mkt%","uniqueChartingRecords","top40RecordWeeks","yearEndSlots","eff"]].round(2).to_string(index=False))
print("\np95 / max realised eff across the decade (>=0.5%% share): %.2f / %.2f" % (big["eff"].quantile(.95), big["eff"].max()))

print("\n=== What SunshinePop's benchmark demands, in the same units ===")
mkt = {1966:1.47, 1967:1.87, 1968:1.42, 1969:0.73}
bench = {1966:4, 1967:10, 1968:4, 1969:12}
for y in (1966,1967,1968,1969):
    realised = m[(m.genre == "SunshinePop") & (m.year == y)]
    rm = float(realised["mkt%"].iloc[0]); rs = float(realised["yearEndSlots"].iloc[0])
    print(f"  {y}: bench {bench[y]:2d} slots on a {mkt[y]:.2f}% market target -> needs eff {bench[y]/mkt[y]:5.2f}"
          f"   | model ran {rm:.2f}% share, {rs:.0f} slots (eff {rs/rm:.2f});"
          f" at the model's p95 eff it would score {big['eff'].quantile(.95)*rm:.1f} slots")

print("\n=== PANEL_LIFECYCLE_PULL vitality multiplier (uncommitted WIP) by genre-year ===")
KF = {  # 1960,62,64,66,67,68,69
 "SunshinePop":[.02,.03,.10,.49,.46,.35,.28], "Bubblegum":[.01,.02,.03,.05,.16,.38,.55],
 "TeenPop":[.44,.42,.46,.43,.31,.26,.21], "EasyListening":[.60,.54,.52,.58,.62,.68,.74],
 "BritishPop":[.01,.02,.65,.50,.43,.35,.30], "Country":None, "Soul":None,
 "TraditionalPop":[.42,.40,.44,.36,.42,.40,.38], "BaroquePop":[.02,.02,.06,.38,.34,.21,.13],
}
KY = [1960,1962,1964,1966,1967,1968,1969]
def base(kf, y):
    for i in range(len(KY)-1):
        if y <= KY[i+1]:
            t = (y-KY[i])/(KY[i+1]-KY[i]); return kf[i] + (kf[i+1]-kf[i])*t
    return kf[-1]
PULL = 0.6
rows = []
for g, kf in KF.items():
    if kf is None: continue
    for y in range(1965, 1970):
        b = base(kf, y); peak = max([b] + [kf[i] for i in range(len(KY)) if KY[i] <= y])
        ret = min(max(b/peak, 0), 1) if peak > 1e-4 else 1
        rows.append(dict(genre=g, year=y, baseline=round(b,3), peakSoFar=round(peak,3),
                         vitality=round(1-PULL*(1-ret), 3)))
v = pd.DataFrame(rows).pivot_table(index="genre", columns="year", values="vitality")
print(v.round(3).to_string())
print("\n(1.000 = no damping. The panel candidacy of every genre in the table is multiplied by this.)")
