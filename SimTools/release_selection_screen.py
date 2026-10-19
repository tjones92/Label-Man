"""Release Judgement Phase 2A paired screen: legacy control against --release-judgement-select=perceived and =fit.

Usage: py SimTools/release_selection_screen.py <control-prefix> <arm-prefix>[,<arm-prefix>...] [<control-prefix> <arms> ...]
Each argument pair is one seed: a control run prefix (shadow on, selection off) and its treatment arms, comma-separated.
Prefixes are paths without the "-records.csv" suffix. Reads records, concentration, single-launch-quality and
release-judgements-shadow. Prints per seed and the paired mean (arm - control):
  - correctness: how orphan singles got their material (pool / fallback) and what was authored;
  - orphan-single realized quality, share above the V1 0.639 cliff, Hot 100 entry and Top 40 rates;
  - chart guards: Hot 100 debuts and Top 10 records a year, CR4 / CR8 / major-family share, entries and Top 40 by tier.
"""
import sys
from io import BytesIO
from pathlib import Path

import pandas as pd

TIERS = ["Major", "MidTier", "Independent", "Boutique", "Small"]


def read(path, cols=None):
    raw = Path(path).read_bytes()
    return pd.read_csv(BytesIO(raw[:raw.rfind(b"\n") + 1]), usecols=cols)


def measure(p):
    r = read(f"{p}-records.csv", ["year", "recordId", "currentPosition", "isPlayerOwned"])
    r = r[(r.currentPosition >= 1) & (~r.isPlayerOwned.astype(str).str.lower().eq("true"))]
    first = r.groupby("recordId").year.min()
    peak = r.groupby("recordId").currentPosition.min()
    q = read(f"{p}-single-launch-quality.csv")
    orphan = q[q.strategy == "OrphanSingle"].copy()
    orphan["entered"] = orphan.singleId.map(peak).le(100)
    orphan["top40"] = orphan.singleId.map(peak).le(40)
    c = read(f"{p}-concentration.csv")
    out = {
        "orphan singles / yr": len(orphan) / orphan.year.nunique(),
        "orphan quality": orphan.diagQuality.mean(),
        "orphan > 0.639 %": 100 * (orphan.diagQuality > 0.639).mean(),
        "orphan Hot 100 entry %": 100 * orphan.entered.mean(),
        "orphan Top 40 %": 100 * orphan.top40.mean(),
        "promo quality": q[q.strategy == "AlbumWithPromo"].diagQuality.mean(),
        "Hot 100 debuts / yr": first.groupby(first).size().mean(),
        "Top 10 records / yr": peak[peak <= 10].index.map(first).value_counts().mean(),
        "CR4": c.c4ChartShare.mean(), "CR8": c.c8ChartShare.mean(), "major family share": c.majorFamilyChartShare.mean(),
    }
    for t in TIERS:
        out[f"entries {t}"] = c[f"chartEntries{t}"].mean()
        out[f"top40 {t}"] = c[f"top40{t}"].mean()
    j = Path(f"{p}-release-judgements-shadow.csv")
    if j.exists():
        s = read(j, ["strategy", "outcome", "selectedSource"])
        o = s[s.strategy == "OrphanSingle"]
        for k, v in o.outcome.value_counts(normalize=True).items():
            out[f"outcome {k} %"] = 100 * v
        pool = o[o.outcome == "pool"]
        for k, v in pool.selectedSource.value_counts(normalize=True).items():
            out[f"pool source {k} %"] = 100 * v
    return pd.Series(out)


def main(argv):
    if len(argv) < 2 or len(argv) % 2:
        print(__doc__)
        return 2
    rows, deltas = {}, []
    for ctl, arms in zip(argv[::2], argv[1::2]):
        base = measure(ctl)
        rows[Path(ctl).name] = base
        for arm in arms.split(","):
            m = measure(arm)
            rows[Path(arm).name] = m
            d = (m - base).rename(Path(arm).name)
            deltas.append(d)
    t = pd.DataFrame(rows)
    pd.set_option("display.width", 250)
    print("Per run")
    print(t.round(3).to_string())
    d = pd.DataFrame(deltas).T
    # Pair arms across seeds by their mode tag (the token after the run tag), e.g. rj2a-per-1001-260 -> per.
    groups = {}
    for col in d.columns:
        groups.setdefault(col.split("-")[1], []).append(col)
    print("\nArm - control, per seed and paired mean")
    out = {}
    for mode, cols in groups.items():
        for c in cols:
            out[c] = d[c]
        out[f"{mode} mean"] = d[cols].mean(axis=1)
    print(pd.DataFrame(out).round(3).to_string())
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
