"""Offline pick-selection study on a no-picks world (`--station-pick-signals`), 1960.

Outcomes come from a run where no station picked, so they are free of pick feedback. Two parts:
1. Separation: per record, how well each meeting-time signal tells a future top 10 from a 41-100 record (AUC).
2. Rules: replay a pick rule at every LA/Minneapolis Top40/FullService meeting (2 slots, age <= 2, unseated,
   launch-exposed, live AI single), and count records a pick lists in either city >= 2 weeks before national debut.
   That count adds to the control's own lead; ranking uses exp(heard) only, not the meeting's common factors
   (format match, freshness, vitality, genre guard, city taste), so it approximates the live pick order.
With several prefixes (one per seed), records are pooled for AUC and lead rates, and each rule is also compared
with the current rule seed by seed (paired mean and SE).
Usage: study_pick_selection.py RUN_PREFIX [RUN_PREFIX ...] OUT_DIR"""
import sys
from pathlib import Path
import numpy as np
import pandas as pd

*prefixes, out = sys.argv[1:]
out = Path(out)
out.mkdir(parents=True, exist_ok=True)
BIG5 = {f"label_000{i}" for i in range(1, 6)}
CITIES = ["Los Angeles", "Minneapolis"]
COHORTS = ["top10-indie", "top10-Big5", "11-40", "41-100", "never"]
SIGNALS = ["z", "nationalLog", "awareness", "push", "negHeld", "negBestRank", "bestScore"]


def cohort(peak, big5):
    if peak <= 10: return "top10-Big5" if big5 else "top10-indie"
    if peak <= 40: return "11-40"
    if peak <= 100: return "41-100"
    return "never"


def auc(pos, neg):
    pos, neg = np.asarray(pos, float), np.asarray(neg, float)
    if not len(pos) or not len(neg): return np.nan
    allv = np.concatenate([pos, neg])
    ranks = pd.Series(allv).rank().to_numpy()
    return (ranks[:len(pos)].sum() - len(pos) * (len(pos) + 1) / 2) / (len(pos) * len(neg))


def separation(rec):
    rows = []
    for group, g in [("all", rec), ("indie", rec[~rec.big5]), ("Big5", rec[rec.big5])]:
        hit, low = g[g.peak.le(10)], g[g.peak.between(41, 100)]
        for sig in SIGNALS:
            rows.append(dict(group=group, signal=sig, top10=len(hit), low=len(low), auc=auc(hit[sig], low[sig])))
    return pd.DataFrame(rows)


def study(pre):
    r = pd.read_csv(pre + "-records.csv", usecols=["week", "year", "recordId", "labelId", "weeksSinceRelease", "currentPosition"])
    r = r[r.year.eq(1960)]
    chart = r[r.currentPosition.between(1, 100)].groupby("recordId").agg(debut=("week", "min"), peak=("currentPosition", "min"))
    s = pd.read_csv(pre + "-station-pick-signals.csv")
    s = s[s.year.eq(1960) & s.fitApplies.eq(1) & s["format"].isin(["Top40", "FullService"])].copy()
    s["big5"] = s.labelId.isin(BIG5)
    s = s.join(chart, on="recordId")

    # ---- 1. Separation, one row per record ----
    young = s[s.age.le(2)]
    rec = young.groupby("recordId").agg(big5=("big5", "first"), tier=("labelTier", "first"), z=("latentZ", "first"),
        nationalLog=("nationalLog", "first"), awareness=("launchAwareness", "first"), push=("push", "first"),
        held=("heldAtLaunch", "first"), bestRank=("localRank", lambda v: v[v > 0].min() if (v > 0).any() else 9999),
        bestScore=("score", "max"), peak=("peak", "first"), debut=("debut", "first"))
    rec["cohort"] = [cohort(p, b) for p, b in zip(rec.peak.fillna(999), rec.big5)]
    rec["negBestRank"] = -rec.bestRank
    rec["negHeld"] = -rec.held

    # ---- 2. Rule replay at the two cities' meetings ----
    m = s[s.city.isin(CITIES) & s.age.le(2) & s.rejection.isna() & s.seatSource.isna() & s.launchExposed.eq(1)].copy()
    m["excess"] = m.nationalLog - m.capLog

    def replay(name, score, keep):
        q = m[keep].assign(rule=score[keep])
        picked = q.sort_values("rule", ascending=False).groupby(["stationId", "week"]).head(2)
        first = picked.groupby("recordId").week.min()
        lead = first[(chart.debut.reindex(first.index) - first).ge(2) | chart.debut.reindex(first.index).isna()]
        led = rec.index.isin(lead[chart.debut.reindex(lead.index).notna()].index)
        res = dict(rule=name, meetings=m.groupby(["stationId", "week"]).ngroups, picks=len(picked),
                   bigFivePicks=int(picked.big5.sum()))
        for c in COHORTS:
            inc = rec.cohort.eq(c)
            res[c + ":n"], res[c + ":led"] = int(inc.sum()), int((led & inc).sum())
        never = rec.cohort.eq("never")
        res["never:picked"] = int(rec.index.isin(first.index)[never].sum())
        return res

    bar, sd = 2.4, 0.5
    heard = m.nationalLog + sd * m.djZ
    base = (heard - m.capLog).ge(bar) & m.heldAtLaunch.eq(1)
    rules = [replay("current: heard, bar 2.4, unserviced", heard, base),
             replay("noise-free fit, bar 2.4, unserviced", m.nationalLog, m.excess.ge(bar) & m.heldAtLaunch.eq(1)),
             replay("current without unserviced", heard, (heard - m.capLog).ge(bar))]
    for w in [1, 2, 4]:
        sc = heard + w * m.launchAwareness
        rules.append(replay(f"heard + {w} x launch awareness, bar on sum", sc, (sc - m.capLog).ge(bar) & m.heldAtLaunch.eq(1)))
    for w in [0.5, 1]:
        sc = heard - w * np.log(m.localRank.clip(lower=1))
        rules.append(replay(f"heard - {w} x log local rank, bar on heard", sc, base))
    # As --station-picks-fit-local-rank implements it: no local rank, no pick.
    ranked = base & m.localRank.gt(0)
    lr = heard - 0.5 * np.log(m.localRank.clip(lower=1))
    rules.append(replay("live local rank 0.5 (unranked out)", lr, ranked))
    if "djArchetype" in m:
        # Indie tastemakers: a Tastemaker jock leaves the big companies' records to their promotion men.
        taste = m.djArchetype.eq("Tastemaker")
        rules.append(replay("live local rank 0.5, Tastemakers skip Big-5", lr, ranked & ~(taste & m.big5)))
        rules.append(replay("live local rank 0.5, Tastemakers skip Majors", lr, ranked & ~(taste & m.labelTier.eq("Major"))))
        rules.append(replay("live local rank 0.5, all stations skip Big-5", lr, ranked & ~m.big5))
    return rec, pd.DataFrame(rules)


def rates(df):
    """Lead rates (%) from summed counts; works on one seed's rows or a pooled sum."""
    res = pd.DataFrame(index=df.index)
    res["picksPerMeeting"] = df.picks / df.meetings
    res["bigFivePickShare"] = 100 * df.bigFivePicks / df.picks.where(df.picks > 0)
    for c in COHORTS:
        res[c] = 100 * df[c + ":led"] / df[c + ":n"].clip(lower=1)
    res["neverPicked%"] = 100 * df["never:picked"] / df["never:n"].clip(lower=1)
    res["ratio41-100/indie"] = res["41-100"] / res["top10-indie"].where(res["top10-indie"] > 0)
    return res


recs, counts = [], []
for pre in prefixes:
    rec, rules = study(pre)
    seed = Path(pre).name
    recs.append(rec.assign(seed=seed))
    counts.append(rules.assign(seed=seed))
rec = pd.concat(recs)
counts = pd.concat(counts, ignore_index=True)

sep = separation(rec)
sep.to_csv(out / "separation.csv", index=False)
rec.groupby("cohort")[["z", "nationalLog", "awareness", "push", "held"]].agg(["mean", "size"]).to_csv(out / "cohort-signals.csv")
pooled = counts.drop(columns="seed").groupby("rule", sort=False).sum(numeric_only=True)
rules = rates(pooled).reset_index()
rules.to_csv(out / "rules.csv", index=False)
pd.set_option("display.width", 250)
print(sep.pivot(index="signal", columns="group", values="auc").round(3))
print(sep.groupby("group")[["top10", "low"]].first())
print(rules.round(2).to_string(index=False))

if len(prefixes) > 1:
    counts.to_csv(out / "rules-by-seed-counts.csv", index=False)
    seedSep = pd.concat([separation(g).assign(seed=s) for s, g in rec.groupby("seed")])
    seedSep.to_csv(out / "separation-by-seed.csv", index=False)
    spread = seedSep.groupby(["group", "signal"]).auc.agg(seedMean="mean", seedSE=lambda v: v.std(ddof=1) / np.sqrt(v.notna().sum()),
                                                         seedMin="min", seedMax="max")
    print("\nAUC across seeds (top 10 vs 41-100)")
    print(spread.round(3).to_string())

    # Paired against the current rule within each seed: rule rate minus current rate, mean and SE over seeds.
    perSeed = pd.concat([rates(g.set_index("rule")).assign(seed=s) for s, g in counts.groupby("seed")]).reset_index()
    perSeed.to_csv(out / "rules-by-seed.csv", index=False)
    current = perSeed[perSeed.rule.str.startswith("current:")].set_index("seed")
    cols = ["top10-indie", "top10-Big5", "41-100", "never", "bigFivePickShare"]
    rows = []
    for rule, g in perSeed.groupby("rule", sort=False):
        d = g.set_index("seed")[cols] - current[cols]
        row = {"rule": rule}
        for c in cols:
            row[c] = f"{d[c].mean():+.1f} ({d[c].std(ddof=1) / np.sqrt(d[c].notna().sum()):.1f})"
        rows.append(row)
    paired = pd.DataFrame(rows)
    paired.to_csv(out / "rules-paired-vs-current.csv", index=False)
    print(f"\nPaired vs current rule, {len(prefixes)} seeds, mean (SE) of per-seed differences, lead pp")
    print(paired.to_string(index=False))
