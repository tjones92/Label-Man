"""LpGenreAllocationFollowup.md §87. Usage from the repo root: python SimTools/analyze_opening_star_owners.py SEEDS ARM [ARM ...]
Big-5 newcomer Top 10 records per seed by year of first Top 10 (history ~4.5/yr), and opening star-archetype acts
(AdultStandardBearer, CurrentHitmaker, EstablishedHitmaker) by label tier at first launch."""
import os, sys
import pandas as pd
B5 = {f"label_000{i}" for i in range(1, 6)}
NEW = {"NewSigning", "Unsigned"}
STAR = {"AdultStandardBearer", "CurrentHitmaker", "EstablishedHitmaker"}
def gz(p): return p if os.path.exists(p) else p + ".gz"
seeds = [int(s) for s in sys.argv[1].split(",")]
for arm in sys.argv[2:]:
    yr = {1960: 0, 1961: 0}; tiers = {}
    for s in seeds:
        p = f"SimLogs/{arm}-{s}-sc-"
        a = pd.read_csv(gz(p + "single-launch-attribution.csv"), usecols=["singleId", "labelId", "career", "openingArchetype", "artistId", "week"]).set_index("singleId")
        q = pd.read_csv(gz(p + "single-launch-quality.csv"), usecols=["singleId", "tier"]).set_index("singleId")
        a = a.join(q)
        r = pd.read_csv(gz(p + "records.csv"), usecols=["year", "recordId", "currentPosition"])
        first = r[r.currentPosition.between(1, 10)].groupby("recordId").year.min()
        nb = a[a.labelId.isin(B5) & a.career.isin(NEW)]
        hits = first.reindex(nb.index).dropna()
        for y in yr: yr[y] += (hits == y).sum()
        st = a[a.openingArchetype.isin(STAR)].sort_values("week").drop_duplicates("artistId")
        st = st.assign(t=st.labelId.isin(B5).map({True: "Big5", False: None}).fillna(st.tier))
        for t, n in st.t.value_counts().items(): tiers[t] = tiers.get(t, 0) + n
    print(arm, "Big-5 newcomer T10 records/seed:", {y: round(v / len(seeds), 2) for y, v in yr.items()},
          "| star-archetype acts by tier at first launch /seed:", {t: round(v / len(seeds), 1) for t, v in sorted(tiers.items())})
