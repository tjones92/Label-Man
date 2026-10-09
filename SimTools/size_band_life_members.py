"""Phases 4e/4f/5 offline sizing (SimTools/BandMemberSimulationDirective.md §14) from a --log-band-life-members run.

Usage:  py SimTools/size_band_life_members.py <run>

Reads SimLogs/<run>-lineup-members.csv (one row per person per act-year, after life events) and reports:
  wealth  -- the stock's spread inside charting groups (writers vs the rest), how many members each reader would touch
  fatigue -- the exhaustion hazard summed over the population on lifetime road years vs the fatigue level, by act kind
  growth  -- the shadow's grown-vs-live technical skill by age band, and the implied shift in act base quality
Diagnostic only; nothing here feeds the sim.
"""
import os
import sys

import numpy as np
import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")

EXHAUSTION_RATE = 0.012
WEALTH_SCALE = 40000.0
STUDIO_AFFORD = 0.45
INDEPENDENCE = 0.12
# Growth decline constants (MemberGrowthService), to put the shadow on the origin-relative decline basis.
VOCAL_DECLINE, VOCAL_START = 0.006, 34
INST_DECLINE, INST_START = 0.004, 50


def norm(w):
    return 1.0 - np.exp(-np.maximum(w, 0) / WEALTH_SCALE)


def main():
    run = sys.argv[1]
    df = pd.read_csv(os.path.join(LOGS, f"{run}-lineup-members.csv"))
    last = df.year.max()
    print(f"{run}: {len(df):,} member-years, years {sorted(df.year.unique())}")

    # ---- wealth --------------------------------------------------------------------------------------------
    w = df[df.year == last].copy()
    w["norm"] = norm(w.wealth)
    groups = w[w.constitution != "Solo"]
    hit = groups[groups.everCharted == 1]
    print("\nWEALTH (last year)")
    for label, sub in [("all members", w), ("charting-group members", hit),
                       ("  writers", hit[hit.writer == 1]), ("  non-writers", hit[hit.writer == 0])]:
        q = sub.wealth.quantile([.5, .9, .99]).round()
        print(f"  {label:24s} n={len(sub):6d}  wealth q50/90/99 {q.tolist()}  norm>={STUDIO_AFFORD}: {(sub.norm >= STUDIO_AFFORD).mean():.1%}")
    gap = []
    for _, act in hit.groupby("artistId"):
        if len(act) < 2:
            continue
        hi, lo = act.wealth.max(), act.wealth.min()
        gap.append(hi / max(lo, 1.0))
    gap = np.array(gap)
    print(f"  within-act richest/poorest ratio, charting groups: q50 {np.median(gap):.2f}  q90 {np.quantile(gap, .9):.2f}  n={len(gap)}")
    inc = df[df.everCharted == 1]
    print(f"  writer income share of income, charting acts: {inc.writerIncome.sum() / max(inc.income.sum(), 1):.1%}")
    print(f"  solo urge added (.12*norm) among leads in charting groups: mean {(INDEPENDENCE * hit[hit.lead == 1].norm).mean():.3f}"
          f"  q90 {(INDEPENDENCE * hit[hit.lead == 1].norm).quantile(.9):.3f}")

    # ---- fatigue -------------------------------------------------------------------------------------------
    print("\nFATIGUE (expected exhaustion events per year: lifetime road years vs fatigue level)")
    active = df[df.lifeState == "Active"].copy()
    age_f = np.clip((active.age - 18) / 16.0, 0.2, 1.3)
    active["h_old"] = EXHAUSTION_RATE * active.roadYears * (1 - active.temperament) * age_f
    active["h_new"] = EXHAUSTION_RATE * active.fatigue * (1 - active.temperament) * age_f
    active["kind"] = np.where(active.signed == 1, np.where(active.everCharted == 1, "signed-charted", "signed-uncharted"),
                              np.where(active.roadLoad >= 0.4, "unsigned-club", "unsigned-other"))
    t = active.groupby(["year", "kind"])[["h_old", "h_new"]].sum().round(1)
    print(t.to_string())
    print("  by year:", active.groupby("year")[["h_old", "h_new"]].sum().round(1).to_dict("index"))
    rich = active[norm(active.wealth) >= STUDIO_AFFORD]
    print(f"  of expected new-hazard exits, share who could afford studio-only: {rich.h_new.sum() / max(active.h_new.sum(), 1e-9):.1%}"
          f"  (charting among them {rich[rich.everCharted == 1].h_new.sum() / max(rich.h_new.sum(), 1e-9):.1%})")

    # ---- growth --------------------------------------------------------------------------------------------
    g = df[(df.lifeState.isin(["Active", "StudioOnly"])) & (df.hours >= 0)].copy()
    first = g.groupby("personId").year.transform("min")
    singer = g.lead == 1
    # The run's shadow used absolute-age decline; rebase to the origin-relative decline the live code uses.
    vocal_abs = VOCAL_DECLINE * np.maximum(0, g.age - VOCAL_START) * (1 + g.roadYears / 8)
    origin_age = g.age - (g.year - first)
    vocal_rel = VOCAL_DECLINE * (np.maximum(0, g.age - VOCAL_START) - np.maximum(0, origin_age - VOCAL_START)) * (1 + g.roadYears / 8)
    inst_abs = INST_DECLINE * np.maximum(0, g.age - INST_START)
    inst_rel = INST_DECLINE * (np.maximum(0, g.age - INST_START) - np.maximum(0, origin_age - INST_START))
    fix = np.where(singer, 0.75 * (vocal_abs - vocal_rel), inst_abs - inst_rel)  # singer tech = mean(power, .5-decline control)
    g["delta"] = g.technicalGrown - g.technicalNow + fix
    g["band"] = pd.cut(g.age, [0, 20, 24, 28, 34, 45, 99], labels=["<=20", "21-24", "25-28", "29-34", "35-45", "46+"])
    print("\nGROWTH (shadow technical minus live, origin-relative decline)")
    print(g.groupby(["year", "band"], observed=True).delta.agg(["count", "mean"]).round(4).to_string())
    by_year = g.groupby("year").delta.mean()
    print("  mean delta by year:", by_year.round(4).to_dict())
    # Base quality = .3 vocal + .25 musicianship + .1 studio (+ .35 writing): technicalSkill reaches it at about
    # .3*.6 (lead only) + .25 + .1*.4, so an act's shift is ~.29 x the mean member delta plus .18 x the lead's.
    acts = g.groupby(["year", "artistId"]).apply(lambda a: 0.29 * a.delta.mean() + 0.18 * a[a.lead == 1].delta.mean()
                                                 if (a.lead == 1).any() else 0.29 * a.delta.mean(), include_groups=False)
    print("  implied act base-quality shift by year:", acts.groupby(level=0).mean().round(4).to_dict())
    print(f"  hours: club acts mean {g[g.roadLoad.between(.44, .46)].weightedHours.mean():.0f} weighted/yr;"
          f" signed charted {g[(g.signed == 1) & (g.everCharted == 1)].weightedHours.mean():.0f}")


if __name__ == "__main__":
    main()
