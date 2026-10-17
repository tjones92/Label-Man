"""Why is the second rank of 1960 Top 10 hitmakers (firms 5-8) thin? Reads 52-week audit logs only.

Per seed: the rank-size curve of Top 10 slot shares, and for each ranked firm its release volume
(launches in release-strategy.csv: orphan singles + albums with a promo single), Hot 100 entries,
Top 10 records, conversion, tier, archetype and template status. Pooled across seeds: volume x conversion
by rank group, and how many non-Big-5 firms reach a given single volume.

Usage: python analyze_second_rank_hitmakers.py PREFIX [PREFIX ...] --output OUT.json
"""
import argparse
import json
from pathlib import Path

import pandas as pd

BIG5 = {"label_0001", "label_0002", "label_0003", "label_0004", "label_0005"}


def firm_table(prefix):
    cols = ["week", "year", "recordId", "labelId", "labelTier", "currentPosition", "quality"]
    parts = [c[c.year.eq(1960) & c.currentPosition.between(1, 100)]
             for c in pd.read_csv(prefix + "-records.csv", usecols=cols, chunksize=200000)]
    rec = pd.concat(parts, ignore_index=True)
    top = rec[rec.currentPosition.le(10)]
    assert len(top) == 520, prefix
    per = rec.groupby("recordId").agg(labelId=("labelId", "first"), peak=("currentPosition", "min"),
                                      q=("quality", "first"))
    rs = pd.read_csv(prefix + "-release-strategy.csv", usecols=["year", "recordId", "labelId", "tier", "chosenFormat"])
    rs = rs[rs.year.eq(1960)]
    d = pd.read_csv(prefix + "-label-directory.csv").set_index("labelId")
    g = pd.DataFrame({
        "launches": rs.groupby("labelId").size(),
        "orphanSingles": rs[rs.chosenFormat.eq("Single")].groupby("labelId").size(),
        "albumsWithPromo": rs[rs.chosenFormat.eq("Album")].groupby("labelId").size(),
        "hot100": per.groupby("labelId").size(),
        "top10Records": per[per.peak.le(10)].groupby("labelId").size(),
        "top40Records": per[per.peak.le(40)].groupby("labelId").size(),
        "top10Slots": top.groupby("labelId").size(),
        "medianQuality": per.groupby("labelId").q.median(),
    }).fillna(0)
    g["tier"] = rs.groupby("labelId").tier.agg(lambda s: s.mode().iat[0])
    g["tier"] = g.tier.fillna(rec.groupby("labelId").labelTier.first())
    g["archetype"] = d.archetype.reindex(g.index)
    g["isHistorical"] = d.isHistorical.reindex(g.index).fillna(False)
    g["name"] = d.labelName.reindex(g.index)
    g["big5"] = g.index.isin(BIG5)
    g = g.sort_values(["top10Slots", "hot100"], ascending=False)
    g["rank"] = range(1, len(g) + 1)
    g.loc[g.top10Slots.eq(0), "rank"] = 999
    return g


def group(rank):
    return "1-4" if rank <= 4 else "5-8" if rank <= 8 else "9-20" if rank <= 20 else "21+" if rank < 999 else "none"


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("prefixes", nargs="+")
    ap.add_argument("--output", required=True)
    a = ap.parse_args()
    tables = []
    out = {"runs": {}}
    for prefix in a.prefixes:
        g = firm_table(prefix)
        g["run"] = Path(prefix).name
        g["group"] = g["rank"].map(group)
        tables.append(g)
        ranked = g[g["rank"] < 999]
        curve = (100 * ranked.top10Slots / 520).round(2).tolist()[:12]
        out["runs"][Path(prefix).name] = {
            "rankSizePct": curve,
            "ranks1to12": ranked.head(12)[["name", "tier", "archetype", "isHistorical", "launches", "hot100",
                                           "top10Records", "top10Slots"]].reset_index().to_dict("records"),
        }
        print(f"\n== {Path(prefix).name}\n  rank-size % (1-12): {curve}")
        for lid, r in ranked.head(12).iterrows():
            print(f"  #{r['rank']:<3}{str(r['name'])[:24]:<25}{r.tier:<12}{str(r.archetype):<18}"
                  f"{'T' if r.isHistorical else ' '} launches={int(r.launches):>3} hot100={int(r.hot100):>3} "
                  f"top10recs={int(r.top10Records):>2} slots={int(r.top10Slots):>3}")
    allg = pd.concat(tables)
    print("\nPooled by rank group (mean per firm):")
    agg = allg.groupby("group").agg(firms=("launches", "size"), launches=("launches", "mean"),
                                    hot100=("hot100", "mean"), top10Records=("top10Records", "mean"),
                                    slots=("top10Slots", "mean"), big5=("big5", "mean"),
                                    historical=("isHistorical", "mean"))
    agg["hot100PerLaunch"] = allg.groupby("group").apply(lambda x: x.hot100.sum() / max(1, x.launches.sum()))
    agg["top10PerHot100"] = allg.groupby("group").apply(lambda x: x.top10Records.sum() / max(1, x.hot100.sum()))
    agg = agg.reindex(["1-4", "5-8", "9-20", "21+", "none"])
    agg["firms"] = agg.firms / len(a.prefixes)
    print(agg.round(3).to_string())
    print("\nRanks 5-8 composition (pooled count of firm-slots):")
    five8 = allg[allg.group.eq("5-8")]
    print(five8.groupby(["big5", "tier"]).size().to_string())
    print("\nNon-Big-5 firms by 1960 launch volume (mean per seed):")
    nb = allg[~allg.big5]
    for lo in (10, 15, 20, 25, 30):
        sub = nb[nb.launches.ge(lo)]
        print(f"  launches >= {lo:>2}: {len(sub) / len(a.prefixes):5.1f} firms, "
              f"top10 recs {sub.top10Records.sum() / len(a.prefixes):5.1f}, slots {sub.top10Slots.sum() / len(a.prefixes):6.1f}")
    print("\nLaunches by tier, per firm (pooled):")
    print(allg.groupby("tier").agg(firms=("launches", "size"), meanLaunches=("launches", "mean"),
                                   p90=("launches", lambda s: s.quantile(.9)), maxLaunches=("launches", "max"),
                                   hot100PerFirm=("hot100", "mean"), top10PerFirm=("top10Records", "mean")).round(2).to_string())
    out["byRankGroup"] = agg.round(4).reset_index().to_dict("records")
    allg.reset_index().to_csv(Path(a.output).with_suffix(".firms.csv"), index=False)
    Path(a.output).write_text(json.dumps(out, indent=2, default=str) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
