"""LpGenreAllocationFollowup.md §98: the --local-promotion screen. Usage from the repo root:
python SimTools/score_local_promotion.py SEEDS ARM [ARM ...]      (SEEDS like 1001,2002,3003,4004)
Needs --regional-home-sales. Per arm, 1961 singles, pooled over seeds:
- home premium per head by label tier x peak class: (home units / home market share) / (away units / away market share),
  weighted over home regions by units. History (1960 KFWB/KDWB/WABC surveys, survey-weeks): small LA indies ~2.5,
  established national indies (Liberty, Dot, Imperial) ~0.9, Majors ~0.94, NY/Philadelphia indies ~0.7.
- per label home region: small labels' home fraction, uncharted home fraction, output / own market.
- guards: small-label share of single units and of Hot 100 record-weeks (1960 / 1961), single and LP units, labels
  defunct at the end (label-finance status)."""
import os, sys
import pandas as pd

SMALL = ["Independent", "Small", "Boutique"]
REG = ["eastcoast", "greatlakes", "westcoast", "greatplains", "deepsouth", "southwest", "rockies"]
def gz(p): return p if os.path.exists(p) else p + ".gz"

def premium(g, mkt):
    num = den = 0.0
    for r, gr in g.groupby("labelHomeRegion"):
        hu = gr[gr.home].units.sum(); au = gr[~gr.home].units.sum()
        if hu <= 0 or au <= 0: continue
        w = gr.units.sum(); num += w * (hu / mkt[r]) / (au / (1 - mkt[r])); den += w
    return num / den if den else float("nan")

def load(arm, seeds):
    fr = []
    for s in seeds:
        d = pd.read_csv(gz(f"SimLogs/{arm}-{s}-sc-regional-home-sales.csv"),
                        usecols=["year", "regionId", "labelHomeRegion", "labelTier", "format", "units", "peakClass"])
        d["seed"] = s; fr.append(d)
    d = pd.concat(fr); d["home"] = d.regionId == d.labelHomeRegion; d["small"] = d.labelTier.isin(SMALL)
    return d

def guards(arm, s):
    p = f"SimLogs/{arm}-{s}-sc-"
    o = {}
    r = pd.read_csv(gz(p + "records.csv"), usecols=["year", "labelId", "currentPosition"])
    tiers = pd.read_csv(gz(p + "label-directory.csv"), usecols=["labelId", "initialTier"]).set_index("labelId").initialTier
    c = r[r.currentPosition.between(1, 100)].copy(); c["small"] = c.labelId.map(tiers).isin(SMALL)
    for y in YEARS: o[f"smallH100%{y}"] = 100 * c[c.year == y].small.mean()
    t = c[c.currentPosition <= 10]
    for y in YEARS: o[f"smallT10%{y}"] = 100 * t[t.year == y].small.mean()
    f = pd.read_csv(gz(p + "label-finance.csv"), usecols=["labelId", "status"])
    o["defunct"] = int((f.groupby("labelId").tail(1).status == "Defunct").sum())
    return o

# --years=1960 restricts the year loops (a 52-week arm against a longer control's first year).
args = [a for a in sys.argv[1:] if not a.startswith("--years=")]
YEARS = next((tuple(int(y) for y in a[8:].split(",")) for a in sys.argv[1:] if a.startswith("--years=")), (1960, 1961))
seeds = args[0].split(",")
for arm in args[1:]:
    d = load(arm, seeds)
    print(f"\n######## {arm}  ({len(seeds)} seeds)")
    for y in YEARS:
        x = d[(d.year == y) & (d.format == "Single")]
        if x.empty: continue
        mkt = x.groupby("regionId").units.sum(); mkt = mkt / mkt.sum()
        rows = []
        for t in ["Small", "Boutique", "Independent", "MidTier", "Major"]:
            row = {"tier": t}
            for pc in ["T40", "H100", "none"]:
                row[pc] = round(premium(x[(x.labelTier == t) & (x.peakClass == pc)], mkt), 2)
            row["all"] = round(premium(x[x.labelTier == t], mkt), 2)
            rows.append(row)
        print(f"-- {y} singles: home premium per head"); print(pd.DataFrame(rows).to_string(index=False))
        sm = x[x.small]
        print(f"   small-label share of single units {100 * sm.units.sum() / x.units.sum():.1f}%  singles total {x.units.sum() / len(seeds) / 1e6:.1f}M/seed  "
              f"LPs {d[(d.year == y) & (d.format == 'Album')].units.sum() / len(seeds) / 1e6:.1f}M/seed")
        if y == 1961:
            print("   region       mkt%  smallHome%  smallUnch%  out/mkt  selfSup%  smallLocal%")
            for r in REG:
                out = x[x.labelHomeRegion == r]; s2 = out[out.small]; u = s2[s2.peakClass == "none"]; inr = x[x.regionId == r]
                hp = lambda g: 100 * g[g.home].units.sum() / max(1, g.units.sum())
                print(f"   {r:12s} {100 * mkt[r]:5.1f} {hp(s2):10.1f} {hp(u):11.1f} {out.units.sum() / inr.units.sum():8.2f} "
                      f"{100 * inr[inr.home].units.sum() / inr.units.sum():9.1f} {100 * inr[inr.home & inr.small].units.sum() / inr.units.sum():11.1f}")
    g = pd.DataFrame([guards(arm, s) for s in seeds])
    print("-- guards (mean of seeds): " + "  ".join(f"{k} {v:.1f}" for k, v in g.mean().items()))
