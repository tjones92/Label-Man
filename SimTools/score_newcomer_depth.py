"""Pooled newcomer / established Big-5 premiums and yearly Big-5 Top 10 share for screen arms.

Example: python SimTools/score_newcomer_depth.py 1001,2002,3003 zfg mnfoot mnfactor mnstock
Arms are run-name prefixes ({arm}-{seed}-sc). Launches in weeks 1-92 are followed through launch +12.
"""
import contextlib
import io
import sys

import pandas as pd

from analyze_big5_chart_opportunities import IDS, simulated, source


def arm_frame(arm, seeds):
    frames, shares, firsts = [], {1960: [0, 0], 1961: [0, 0]}, [0, 0]
    for s in seeds:
        run = f"{arm}-{s}-sc"
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            a = simulated(run)
        text = out.getvalue()
        b, n = text.split("1961 first-act records B/all ")[1].split()[:2]
        firsts[0] += int(b); firsts[1] += int(n)
        r = pd.read_csv(source(run, "records.csv"), usecols=["year", "labelId", "currentPosition"])
        t = r[r.currentPosition.between(1, 10)]
        for y in shares:
            ty = t[t.year.eq(y)]
            shares[y][0] += ty.labelId.isin(IDS).sum(); shares[y][1] += len(ty)
        a = a[a.week.between(1, 92)].copy()
        for col in ("debut", "t10"):
            a[col] = a[col].sub(a.week).between(0, 12)
        a["seed"] = s
        frames.append(a)
    return pd.concat(frames), shares, firsts


def premiums(a):
    row = {}
    for name, g in (("new", a[a.new]), ("est", a[~a.new])):
        b, f = g[g.big], g[~g.big]
        row[f"{name} B T10/launch"] = f"{b.t10.sum()}/{len(b)}"
        row[f"{name} F T10/launch"] = f"{f.t10.sum()}/{len(f)}"
        row[f"{name} entryRR"] = round(b.debut.mean() / f.debut.mean(), 2)
        row[f"{name} T10RR"] = round(b.t10.mean() / f.t10.mean(), 2)
        row[f"{name} T10RR by seed"] = " / ".join(
            f"{x[x.big].t10.mean() / x[~x.big].t10.mean():.2f}" for _, x in g.groupby("seed"))
    b, f = a[a.big], a[~a.big]
    row["all T10RR"] = round(b.t10.mean() / f.t10.mean(), 2)
    return row


if __name__ == "__main__":
    seeds = [int(s) for s in sys.argv[1].split(",")]
    rows = {}
    for arm in sys.argv[2:]:
        a, shares, firsts = arm_frame(arm, seeds)
        row = premiums(a)
        for y, (b, n) in shares.items():
            row[f"Big-5 T10 share {y}"] = round(b / n, 3)
        row["1961 Big-5 first-act hits"] = f"{firsts[0]}/{firsts[1]}"
        rows[arm] = row
    print(pd.DataFrame(rows).to_string())
    print("History: Big-5 T10 share 1960 0.342, 1961 0.206; 1961 Big-5 first-act hits 3/44")
