"""Score finished 104-week runs. Example: python SimTools/score_dj_tip_experiment.py djtip-2002-sc"""
from collections import defaultdict
import json
import sys

import pandas as pd

from analyze_big5_chart_opportunities import LOG, simulated, source
from analyze_response_auditions import analyze


def verify_tip_paths(run, a):
    log = pd.read_json(source(run, "response-auditions.jsonl"), lines=True)
    tips = log.loc[log.sourceStation.notna()].sort_values("week").copy()
    playlists = pd.read_csv(source(run, "station-playlists.csv"),
                            usecols=["week", "stationId", "regionId", "recordId", "city"])
    cities = playlists.groupby("stationId").city.first()
    regions = playlists.groupby("stationId").regionId.first()
    plays = pd.MultiIndex.from_frame(playlists[["week", "stationId", "recordId"]])
    # A week-W offer reads a report from W-1, prepared before the W-1 meeting.
    # The sender must have played the record on the two preceding published lists.
    for lag in (2, 3):
        keys = pd.MultiIndex.from_arrays([tips.week - lag, tips.sourceStation, tips.record])
        assert keys.isin(plays).all(), "A DJ recommended a title without playing it"
    assert tips.sourceStation.map(cities).ne(tips.station.map(cities)).all()
    assert tips.sourceStation.map(regions).eq(tips.sourceRegion).all()
    assert tips.station.map(regions).eq(tips.region).all()
    starts = tips[tips.seat.eq("research-trial")]
    if "retailUnchanged" in starts:
        assert starts.retailUnchanged.eq(True).all(), "A radio audition changed retail inventory, held stock or arrival"
    # Trace only a second-hand recommendation for which the sender actually took a
    # tip, graduated on local evidence and subsequently became the named witness.
    graduates = log[log.seat.eq("research-graduated")].groupby(["station", "record"]).week.min().to_dict()
    hops, relays = {}, []
    for tip in tips.itertuples():
        sender = (tip.sourceStation, tip.record)
        depth = 1
        if sender in hops and graduates.get(sender, float("inf")) < tip.sourceWeek:
            depth = hops[sender] + 1
            relays.append((depth, tip.seat == "research-trial"))
        if tip.seat == "research-trial":
            hops[(tip.station, tip.record)] = depth
    pre = starts.merge(a, left_on="record", right_index=True, suffixes=("", "_launch"), validate="many_to_one")
    pre = pre[pre.new & pre.week_launch.le(92) & pre.week.le(pre.week_launch + 12)
              & (pre.debut.isna() | pre.week.lt(pre.debut))]
    print("Verified DJ senders", tips.sourceStation.nunique(), "contacts", tips.groupby(["sourceStation", "station"]).ngroups,
          "same-region tips", tips.sourceRegion.eq(tips.region).sum(), "relay tips", len(relays),
          "relay starts", sum(start for _, start in relays), "longest admitted chain", max(hops.values(), default=0),
          "pre-entry newcomer trial starts by Big-5 and retail gate",
          pre.groupby(["big", pre.geography.fillna("open")]).size().to_dict())


def score(run):
    receipt = (LOG / "writing-calibration" / f"{run}.log").read_text(encoding="utf-8-sig", errors="replace")
    assert "CHART_AUDIT_COMPLETE" in receipt, f"Incomplete {run}"
    flags = json.loads((LOG / "writing-calibration" / f"{run}-request.json").read_text(encoding="utf-8-sig"))["flags"]
    a = simulated(run)
    if any(f.split("=")[0] == "--dj-word-of-mouth" for f in flags):
        verify_tip_paths(run, a)
        audit = analyze(dict(name=run, arm="djtip", seed=int(run.split("-")[1]), weeks=104))
        cols = ["namedTips", "starts", "graduated", "geographicStarts", "geographicGraduations",
                "heldRegionStarts", "heldRegionGraduations", "sustainedOrdinary"]
        print("Airplay", {k: audit[k] for k in cols})
    m = pd.read_csv(source(run, "market-clearing-weekly.csv"))
    assert m.week.nunique() == 104 and not m.duplicated(["regionId", "week"]).any()
    assert m[["inventoryViolationCount", "allocationViolationCount", "reconciliationDelta", "settlementDelta"]].eq(0).all().all()
    a = a[a.week.between(1, 92)].copy()
    for col in ("debut", "t10"):
        a[col] = a[col].sub(a.week).between(0, 12)
    return a


def table(a):
    rows = {}
    for label, group in [("all", a), ("new", a[a.new]), ("other careers", a[~a.new])]:
        b, f = group[group.big], group[~group.big]
        rows[label] = dict(bigLaunch=len(b), bigEntry=b.debut.sum(), bigTop=b.t10.sum(),
                           fieldLaunch=len(f), fieldEntry=f.debut.sum(), fieldTop=f.t10.sum(),
                           entryRR=b.debut.mean() / f.debut.mean(),
                           climbRR=(b.t10.sum() / b.debut.sum()) / (f.t10.sum() / f.debut.sum()),
                           topRR=b.t10.mean() / f.t10.mean())
    return pd.DataFrame(rows).T.round(3).to_string()


if __name__ == "__main__":
    groups = defaultdict(list)
    for run in sys.argv[1:]:
        frame = score(run)
        print(run, table(frame), sep="\n")
        groups[run.split("-")[0]].append(frame)
    for arm, frames in groups.items():
        print(arm, "pooled", table(pd.concat(frames)), sep="\n")
