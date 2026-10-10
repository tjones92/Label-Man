"""Live income, room growth credit and club nights against a stored reference
(SimTools/LocalSceneLiveCalibrationDirective.md §5).

usage: py SimTools/analyze_scene_live_income.py TREATMENT_TAG REF_TAG [SEED ...]
  e.g. py SimTools/analyze_scene_live_income.py live-inc-v1 live-cal-v1-flat 1001 2002
Treatment files: SimLogs/<TREATMENT_TAG>-live-<seed>-*.csv. Reference files: SimLogs/ref-*-<seed>-*/<REF_TAG>-<seed>-*.csv
or SimLogs/<REF_TAG>-<seed>-*.csv.
"""
import pathlib
import sys

import pandas as pd

LOGS = pathlib.Path(__file__).resolve().parent.parent / "SimLogs"


def find(prefix, suffix):
    hits = list(LOGS.glob(f"{prefix}-{suffix}")) + list(LOGS.glob(f"ref-*/{prefix}-{suffix}"))
    if not hits:
        raise FileNotFoundError(f"{prefix}-{suffix}")
    return hits[0]


def departures(prefix):
    ev = pd.read_csv(find(prefix, "lineup-events.csv"))
    ev = ev[ev.applied.astype(str).str.lower() == "true"]
    return ev[ev.kind != "None"].groupby(["event", "kind"]).size()


def main():
    trt_tag, ref_tag, seeds = sys.argv[1], sys.argv[2], sys.argv[3:] or ["1001", "2002"]
    for seed in seeds:
        trt, ref = f"{trt_tag}-live-{seed}", f"{ref_tag}-{seed}"
        print(f"\n=== seed {seed}: {trt} vs {ref}")
        members = pd.read_csv(find(trt, "lineup-members.csv"))
        m = members[members.year == 1960].copy()
        print("live income per member-year, 1960 (gross):")
        print(m.groupby("signed").liveIncome.describe(percentiles=[.5, .9, .99]).round(0).to_string())
        print(f"  live share of all income: {(.25 * m.liveIncome.sum()) / max(m.income.sum(), 1):.1%} (the saved quarter)")
        print("  wealth, 1960:", m.wealth.describe(percentiles=[.5, .9, .99]).round(0).to_dict())
        d = pd.concat({"ref": departures(ref), "trt": departures(trt)}, axis=1).fillna(0).astype(int)
        d["delta"] = d.trt - d.ref
        print("departures by kind (applied):")
        print(d.to_string())
        rooms = pd.read_csv(find(trt, "scene-room-years-end.csv"))
        print("club sets per year:", rooms[rooms.kind == "Club"].groupby("year").sets.sum().to_dict())
        work = pd.read_csv(find(trt, "scene-work-end.csv"))
        w60 = work[work.year == 1960]
        print(f"unbudgeted room hours, 1960: {w60.unbudgetedHours.sum():.0f} of {w60.stageHours.sum():.0f}")
        for name in ("decade-annual-rollup.csv",):
            r, t = pd.read_csv(find(ref, name)), pd.read_csv(find(trt, name))
            num = [c for c in r.columns if c in t.columns and pd.api.types.is_numeric_dtype(r[c]) and c != "year"]
            delta = ((t[num].sum() - r[num].sum()) / r[num].sum().replace(0, float("nan")) * 100).round(2)
            print("annual rollup, % change (2 years summed), largest moves:")
            print(delta.abs().sort_values(ascending=False).head(12).index.map(lambda c: f"  {c}: {delta[c]:+.2f}%").str.cat(sep="\n"))


if __name__ == "__main__":
    main()
