"""Score the stacked-quality-response screen (Big5ChartOpportunityInvestigation.md, 2026-11-17).

Example: python SimTools/score_quality_response.py qctl52-2002-sc qwom52-2002-sc qpk2x52-2002-sc
The first run is the control. Launches are AI singles in weeks 1..(last week - 12); each is followed through launch +12.
"""
import contextlib
import io
import json
import sys

import numpy as np
import pandas as pd

from analyze_big5_chart_opportunities import LOG, IDS, simulated, source

pd.set_option("display.width", 250)
pd.set_option("display.max_columns", 40)


def logit(df, cols, y):
    X = np.column_stack([np.ones(len(df))] + [df[c].astype(float).values for c in cols])
    Y = df[y].astype(float).values
    b = np.zeros(X.shape[1])
    for _ in range(60):
        p = 1 / (1 + np.exp(-X @ b))
        H = X.T @ (X * (p * (1 - p))[:, None])
        b += np.linalg.solve(H, X.T @ (Y - p))
    se = np.sqrt(np.diag(np.linalg.inv(H)))
    return dict(zip(["const"] + cols, zip(b.round(2), se.round(2))))


def one(run):
    receipt = (LOG / "writing-calibration" / f"{run}.log").read_text(encoding="utf-8-sig", errors="replace")
    assert "CHART_AUDIT_COMPLETE" in receipt, f"Incomplete {run}"
    with contextlib.redirect_stdout(io.StringIO()):
        a = simulated(run)
    r = pd.read_csv(source(run, "records.csv"), usecols=["week", "recordId", "labelId", "genre", "currentPosition", "unitsThisWeek"])
    last = r.week.max()
    a = a[a.week.between(1, last - 12)].copy()
    q = pd.read_csv(source(run, "single-launch-quality.csv"), usecols=["singleId", "runtimeQuality"]).set_index("singleId")
    a["q"] = q.runtimeQuality.reindex(a.index)
    # records.csv logs only some records, so launch coverage is unavailable for most misses; the models omit it.
    out_missing = round(a.q.isna().mean(), 3)
    a = a.dropna(subset=["q"])
    a["lq"] = np.log(a.q)
    for col in ("debut", "t10"):
        a[col] = a[col].sub(a.week).between(0, 12)
    out = {"run": run, "weeks": last, "missing q": out_missing}
    for name, g in (("all", a), ("new", a[a.new])):
        b, f = g[g.big], g[~g.big]
        out[f"{name} B T10/launch"] = f"{b.t10.sum()}/{len(b)}"
        out[f"{name} F T10/launch"] = f"{f.t10.sum()}/{len(f)}"
        out[f"{name} entryRR"] = round(b.debut.mean() / f.debut.mean(), 2)
        out[f"{name} T10RR"] = round(b.t10.mean() / f.t10.mean(), 2)
    new = a[a.new]
    out["new H100 lq slope"] = logit(new, ["lq"], "debut")["lq"]
    m = logit(new, ["big", "lq"], "debut")
    out["new H100 big|lq"] = m["big"]
    out["new H100 lq|big"] = m["lq"]
    out["new T10 lq|big"] = logit(new, ["big", "lq"], "t10")["lq"]
    out["all T10 lq|big"] = logit(a, ["big", "lq"], "t10")["lq"]
    chart = r[r.currentPosition.between(1, 100)]
    out["single units (M)"] = round(r[r.recordId.isin(a.index)].unitsThisWeek.sum() / 1e6, 2)
    out["all-record units (M)"] = round(r.unitsThisWeek.sum() / 1e6, 2)
    t10 = chart[chart.currentPosition <= 10]
    out["Big-5 T10 slot share"] = round(t10.labelId.isin(IDS).mean(), 3)
    out["distinct T10 records"] = t10.recordId.nunique()
    out["T40 genre share"] = chart[chart.currentPosition <= 40].genre.value_counts(normalize=True)
    return out


if __name__ == "__main__":
    rows = [one(run) for run in sys.argv[1:]]
    genres = pd.DataFrame({row["run"]: row.pop("T40 genre share") for row in rows}).fillna(0)
    print(pd.DataFrame(rows).set_index("run").T.to_string())
    control = genres.columns[0]
    genres = genres[genres[control].ge(.02) | genres.max(axis=1).ge(.02)]
    for run in genres.columns[1:]:
        genres[f"{run} - ctl"] = genres[run] - genres[control]
    print("\nTop 40 slot-week genre share (genres >= 2%)\n", (100 * genres).round(1).sort_values(control, ascending=False).to_string())
