"""Celebrity fanbase Phase 0 calibration read (SimTools/CelebrityFanbaseDesign.md §9, §10).

Reads <run>-following.csv written by --observe-following and reports, per year:
  - following sizes (acts over 10k / 100k / 1M, the top five, young vs adult) against "top teen act 1-3M";
  - the fan-club figure a manager would report (3% of the following) against "50-80k";
  - would-be advance orders at release against "top act 1-2M in a phenomenon year; solid fanbase act 100-300k";
  - following decay after an act's last Top 40 week, young and adult, against "2-3 years young, 5-7 adult";
  - salience: the leader's share and its lead over the runner-up, against a dominance bar of 3x at 8%;
  - acts entering sensation and phenomenon states, against 10-25 and 0-1 a decade.

Usage: python SimTools/analyze_following.py SimLogs/<run>/<run>-following.csv
"""
import sys

import numpy as np
import pandas as pd

FAN_CLUB_SHARE = 0.03



def main(path):
    df = pd.read_csv(path)
    dates = pd.to_datetime(df["date"], format="%m/%d/%Y")
    df["year"] = dates.dt.year
    df["total"] = df["young"] + df["adult"]
    df["day"] = (dates - pd.Timestamp(1960, 1, 1)).dt.days
    acts = df[df.kind == "act"].copy()
    rel = df[df.kind == "release"].copy()
    week = df[df.kind == "week"].copy()

    print("== Following sizes (each act's largest monthly row in the year) ==")
    for year, g in acts.groupby("year"):
        best = g.sort_values("total").groupby("artistId").tail(1).sort_values("total", ascending=False)
        top = best.head(5)
        names = "; ".join(f"{r.artist} {r.young / 1e3:.0f}k/{r.adult / 1e3:.0f}k" for r in top.itertuples())
        print(f"{year}: acts>=1k {len(best)}  >=10k {(best.total >= 1e4).sum()}  >=100k {(best.total >= 1e5).sum()}  >=1M {(best.total >= 1e6).sum()}"
              f"  top {best.total.iloc[0] / 1e3:.0f}k (club {best.total.iloc[0] * FAN_CLUB_SHARE / 1e3:.1f}k)  [{names}]")

    print("\n== Would-be advance orders at release (young x per-follower + adult x per-follower) ==")
    rel["value"] = pd.to_numeric(rel["value"], errors="coerce")
    rel["format"] = rel["detail"].str.split("|").str[0]
    # "A solid fanbase act" is read as the year's 20 largest followings (each act's largest monthly row).
    top20 = {year: set(g.sort_values("total").groupby("artistId").tail(1).nlargest(20, "total").artistId)
             for year, g in acts.groupby("year")}
    for (year, fmt), g in rel.groupby(["year", "format"]):
        solid = g[g.total >= 5e4]
        head = g[g.artistId.isin(top20.get(year, set()))]
        print(f"{year} {fmt:6s}: releases {len(g):5d}  max {g.value.max() / 1e3:7.1f}k  p99 {g.value.quantile(.99) / 1e3:6.1f}k"
              f"  median(following>=50k) {solid.value.median() / 1e3 if len(solid) else 0:6.1f}k (n={len(solid)})"
              f"  median(top-20 followings) {head.value.median() / 1e3 if len(head) else 0:6.1f}k (n={len(head)})")
    loss = rel["detail"].str.extract(r"loss=([0-9.]+)")[0].astype(float)
    print(f"disappointment: {(loss > 0).mean():.1%} of releases by followed acts, mean loss when hit {loss[loss > 0].mean() if (loss > 0).any() else 0:.3f}")

    print("\n== Decay after the last Top 40 week (acts with >=10k at that point) ==")
    rows = []
    for aid, g in acts.groupby("artistId"):
        g = g.sort_values("day")
        last = g.lastTop40Day.max()
        if last < 0:
            continue
        at = g[g.day >= last].head(1)
        if at.empty or at.total.iloc[0] < 1e4:
            continue
        y0, a0, d0 = at.young.iloc[0], at.adult.iloc[0], at.day.iloc[0]
        for years in (1, 2, 3):
            later = g[g.day >= d0 + 365 * years].head(1)
            if later.empty:
                continue
            rows.append((years, later.young.iloc[0] / y0 if y0 > 0 else np.nan, later.adult.iloc[0] / a0 if a0 > 0 else np.nan))
    if rows:
        r = pd.DataFrame(rows, columns=["years", "young", "adult"])
        for years, g in r.groupby("years"):
            ym, am = g.young.median(), g.adult.median()
            hy = years * np.log(0.5) / np.log(ym) if 0 < ym < 1 else np.inf
            ha = years * np.log(0.5) / np.log(am) if 0 < am < 1 else np.inf
            print(f"+{years}y: n={len(g)}  young kept {ym:.2f} (half-life {hy:.1f}y)  adult kept {am:.2f} (half-life {ha:.1f}y)")
    else:
        print("no act has a year of rows after its last Top 40 week yet")

    print("\n== Salience leader by year ==")
    week["value"] = pd.to_numeric(week["value"], errors="coerce")
    week["lead"] = week["detail"].str.extract(r"lead=([0-9.]+)")[0].astype(float)
    week["second"] = week["detail"].str.extract(r"second=([0-9.]+)")[0].astype(float)
    for year, g in week.groupby("year"):
        top = g.loc[g.value.idxmax()]
        print(f"{year}: leader max {g.value.max():.4f} ({top.artist})  median {g.value.median():.4f}  lead median {g.lead.median():.2f} max {g.lead.max():.2f}"
              f"  weeks>=8% {(g.value >= 0.08).sum()}  weeks lead>=3 {(g.lead >= 3).sum()}  weeks both {((g.value >= 0.08) & (g.lead >= 3)).sum()}")

    print("\n== Attention states reached (first year an act's peak column shows it) ==")
    for state in ("NationalSensation", "CulturalPhenomenon"):
        first = acts[acts.peak == state].groupby("artistId").year.min()
        counts = first.value_counts().sort_index()
        print(f"{state}: {len(first)} acts  " + "  ".join(f"{y}:{n}" for y, n in counts.items()))


if __name__ == "__main__":
    main(sys.argv[1])
