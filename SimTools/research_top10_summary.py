"""Cross-year table of the Billboard Top 10 reconstructions (HistoricalLabelTierBalanceResearch.md §13).

Usage: research_top10_summary.py OUT_CSV YEAR [YEAR ...]

Reads SimLogs/historical-top10-YEAR/concentration_YEAR.csv and records_YEAR_grouped.csv for each year. Adds a
record-level bootstrap SD of the operating-label slot CR4/CR8 (records resampled with replacement, 2,000 draws,
seed 1960): the sampling noise of one year's 52 charts, which sets how tight a single-year band can be.
"""
import sys

import numpy as np
import pandas as pd

OUT, YEARS = sys.argv[1], [int(y) for y in sys.argv[2:]]
PB = {1960: (28, 52, 39), 1961: (27, 48, 39), 1962: (25, 46, 41), 1963: (26, 55, 36), 1964: (34, 51, 37),
      1965: (37, 61, 35), 1966: (38, 61, 31), 1967: (40, 60, 35), 1968: (42, 61, 30), 1969: (42, 64, 31)}


def cr(slots_by_firm):
    s = np.sort(slots_by_firm)[::-1]
    return 100 * s[:4].sum() / s.sum(), 100 * s[:8].sum() / s.sum()


def bootstrap(p, key, n=2000, seed=1960):
    rng = np.random.default_rng(seed)
    firms = pd.factorize(p[key])[0]
    slots = p.slots.to_numpy()
    out = np.empty((n, 2))
    for i in range(n):
        idx = rng.integers(0, len(p), len(p))
        out[i] = cr(np.bincount(firms[idx], weights=slots[idx]))
    return out.std(axis=0)


def main():
    rows = []
    for y in YEARS:
        d = f"SimLogs/historical-top10-{y}"
        c = pd.read_csv(f"{d}/concentration_{y}.csv")
        g = pd.read_csv(f"{d}/records_{y}_grouped.csv")
        sd4, sd8 = bootstrap(g, "label")
        prov = g.labelSource.str.split(" ").str[0].str.replace(r"[^A-Za-z]", "", regex=True).value_counts()
        for r in c.itertuples():
            rows.append({**r._asdict(), "PB_CR4": PB[y][0], "PB_CR8": PB[y][1], "PB_firms": PB[y][2],
                         "labelBootSD_CR4": round(sd4, 1), "labelBootSD_CR8": round(sd8, 1),
                         "records_n": len(g), "printRows": int(prov.get("print", 0)),
                         "infoboxRows": int(prov.get("wikipedia", 0)),
                         "unverifiedRows": int(prov.get("UNVERIFIED", 0) + prov.get("UNRESOLVED", 0))})
    out = pd.DataFrame(rows).drop(columns="Index")
    out.to_csv(OUT, index=False)
    view = out[(out.records == "all") & (out.unit == "slots")]
    print(view[["year", "grouping", "CR4", "CR8", "firms5to8", "firms", "oneHitFirms", "PB_CR4", "PB_CR8",
                "PB_firms", "labelBootSD_CR4", "labelBootSD_CR8"]].to_string(index=False))
    pb = out[(out.records == "exclCarryover") & (out.unit == "slots") & (out.grouping == "affiliated")]
    print("\nPeterson-Berger basis (any affiliation, slots, excl. carryover):")
    print(pb[["year", "CR4", "CR8", "firms", "oneHitFirms", "PB_CR4", "PB_CR8", "PB_firms"]].to_string(index=False))


if __name__ == "__main__":
    main()
