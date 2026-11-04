"""Trace annual head hits to release volume, chart entry, and Top 10 conversion.

Reads completed or stopped audit CSVs. Annual visitor counts include carryover;
conversion uses actual orphan/promo single IDs released in the first 48 weeks
of each year, followed for eight weeks. Never divides annual visitors by an
unrelated release cohort. Source prefixes and observation limits are retained.
"""
import argparse
import json
from io import BytesIO
from pathlib import Path

import pandas as pd


SEEDS = (1001, 2002, 3003, 4004, 5005)
BIG5 = {f"label_000{i}" for i in range(1, 6)}
LATER = {"EnabledInitialReserve", "RuntimeFormation"}


def audit_year(weeks):
    """The chart audit groups 52 weeks, independently of date-year boundaries."""
    return 1960 + (weeks - 1) // 52


def require_complete_chart(chart, through):
    if chart.duplicated(["week", "recordId"]).any():
        raise ValueError("A chart contains duplicate record/week observations")
    counts = chart.groupby("week").currentPosition.agg(["size", "nunique"]).reindex(range(1, through + 1), fill_value=0)
    incomplete = counts[~counts.eq(100).all(axis=1)]
    if len(incomplete):
        raise ValueError(f"Need all 100 ranks in every week through {through}; incomplete: {list(incomplete.index)}")


def read_csv(prefix, suffix, columns=None):
    raw = Path(f"{prefix}-{suffix}.csv").read_bytes()
    # A stopped writer may leave a partial last row, including an unclosed quote.
    return pd.read_csv(BytesIO(raw[:raw.rfind(b"\n") + 1]), usecols=columns)


def single_launches(strategy):
    """Album decisions identify the actual single with promoSingleId."""
    orphan = strategy[strategy.chosenFormat.eq("Single")].copy()
    orphan["singleId"] = orphan.recordId
    promo = strategy[strategy.strategy.eq("AlbumWithPromo")].copy()
    if promo.promoSingleId.isna().any():
        raise ValueError("An AlbumWithPromo decision has no promo single identity")
    promo["singleId"] = promo.promoSingleId
    launches = pd.concat([orphan, promo], ignore_index=True)
    if launches.singleId.duplicated().any():
        raise ValueError("A single launch must appear only once")
    return launches


def conversion_cohorts(launches, chart, through=160, window=8, release_weeks=48):
    launches = launches.copy()
    starts = 1 + (launches.year - 1960) * 52
    launches["withinYearWeek"] = launches.week - starts + 1
    eligible = launches[launches.withinYearWeek.between(1, release_weeks) &
                        (launches.week + window - 1 <= through)].copy()
    observations = chart[["recordId", "week", "currentPosition"]].merge(
        eligible[["singleId", "week"]].rename(columns={"week": "launchWeek"}),
        left_on="recordId", right_on="singleId", validate="many_to_one")
    observations = observations[(observations.week >= observations.launchWeek) &
                                (observations.week < observations.launchWeek + window)]
    peaks = observations.groupby("singleId").currentPosition.min()
    eligible["peakInWindow"] = eligible.singleId.map(peaks)
    eligible["hot100"] = eligible.peakInWindow.between(1, 100)
    eligible["top10"] = eligible.peakInWindow.between(1, 10)
    return eligible


def summarize(group):
    n, entries, hits = len(group), int(group.hot100.sum()), int(group.top10.sum())
    entered = group[group.hot100]
    return dict(launches=n, hot100=entries, top10=hits,
                entryRate=entries / n if n else None,
                conversionGivenEntry=hits / entries if entries else None,
                hitYield=hits / n if n else None,
                estimatedQuality=float(group.qualityEstimate.mean()),
                chartedActualQuality=float(entered.quality.mean()),
                promoPct=float(100 * group.strategy.eq("AlbumWithPromo").mean()),
                starPlusPct=float(100 * group.careerState.isin(["Star", "Superstar"]).mean()),
                laterPopulationPct=float(100 * group.cohort.isin(LATER).mean()),
                launchRecognitionOfCharted=float(entered.launchEffectiveRecognition.mean()),
                launchStockOfCharted=float(entered.initialLaunchStock.mean()))


def summarize_groups(frame, keys):
    d = frame.assign(promo=frame.strategy.eq("AlbumWithPromo"),
                     star=frame.careerState.isin(["Star", "Superstar"]), later=frame.cohort.isin(LATER),
                     chartQuality=frame.quality.where(frame.hot100),
                     chartRecognition=frame.launchEffectiveRecognition.where(frame.hot100),
                     chartStock=frame.initialLaunchStock.where(frame.hot100))
    g = d.groupby(keys).agg(launches=("singleId", "size"), hot100=("hot100", "sum"), top10=("top10", "sum"),
                           estimatedQuality=("qualityEstimate", "mean"), chartedActualQuality=("chartQuality", "mean"),
                           promoPct=("promo", "mean"), starPlusPct=("star", "mean"), laterPopulationPct=("later", "mean"),
                           launchRecognitionOfCharted=("chartRecognition", "mean"), launchStockOfCharted=("chartStock", "mean"))
    g["entryRate"] = g.hot100 / g.launches
    g["conversionGivenEntry"] = g.top10 / g.hot100.replace(0, float("nan"))
    g["hitYield"] = g.top10 / g.launches
    g[["promoPct", "starPlusPct", "laterPopulationPct"]] *= 100
    return g


def decompose(old, new):
    """Exact sequential accounting: N * entry rate * conditional conversion."""
    n0, n1 = old["launches"], new["launches"]
    e0, e1 = old["entryRate"], new["entryRate"]
    c0, c1 = old["conversionGivenEntry"], new["conversionGivenEntry"]
    if None in (e0, e1, c0, c1):
        return None
    pieces = dict(volume=(n1 - n0) * e0 * c0,
                  chartEntry=n1 * (e1 - e0) * c0,
                  conditionalTop10=n1 * e1 * (c1 - c0))
    assert abs(sum(pieces.values()) - (new["top10"] - old["top10"])) < 1e-8
    return dict(hitChange=new["top10"] - old["top10"], **pieces)


def quality_standardization(control, candidate):
    """Compare conversion using the control's entrant-quality distribution."""
    bins = [0, .5, .6, .65, .7, .75, .8, .85, 1.01]
    groups = []
    for frame in (control, candidate):
        entered = frame[frame.hot100].copy()
        if entered.quality.isna().any():
            raise ValueError("Every chart entrant needs observed actual quality")
        entered["qualityBin"] = pd.cut(entered.quality, bins, include_lowest=True)
        if entered.qualityBin.isna().any():
            raise ValueError("Actual entrant quality lies outside the declared bins")
        grouped = entered.groupby("qualityBin", observed=True).agg(entries=("singleId", "size"), hits=("top10", "sum"))
        grouped["rate"] = grouped.hits / grouped.entries
        groups.append(grouped)
    d, t = groups
    common = d.index.intersection(t.index)
    covered = int(d.loc[common, "entries"].sum())
    return dict(bins=bins, controlEntrants=int(d.entries.sum()), controlEntrantsCovered=covered,
                controlConditionalRate=float(d.hits.sum() / d.entries.sum()) if len(d) else None,
                candidateConditionalRate=float(t.hits.sum() / t.entries.sum()) if len(t) else None,
                candidateRateWithControlQualityMix=float((d.loc[common, "entries"] * t.loc[common, "rate"]).sum() / covered) if covered else None,
                note="Descriptive standardization over chart entrants; not a causal estimate and not all-release quality.")


def sampled_artist_heat(chart, heat):
    if heat.duplicated(["week", "artistId"]).any():
        raise ValueError("Duplicate artist/week heat sample")
    # Artists absent from the cache have zero authored heat. Keep their slots;
    # an inner join would overstate the mean by dropping newly charting acts.
    sampled = chart[chart.week.isin(heat.week.unique())].merge(
        heat, on=["week", "artistId"], how="left", validate="many_to_one")
    values = [c for c in heat.columns if c not in ("week", "artistId")]
    sampled[values] = sampled[values].fillna(0)
    return sampled


def add_attribution(prefix, eligible, chart, annual):
    """Optional telemetry includes unsuccessful launches and monthly heat samples."""
    if not Path(f"{prefix}-single-launch-attribution.csv").exists():
        return
    actual = read_csv(prefix, "single-launch-attribution")
    if actual.singleId.duplicated().any():
        raise ValueError("Duplicate launch attribution identity")
    actual = eligible[["singleId", "year", "hot100", "top10"]].merge(
        actual.drop(columns=["year"]), on="singleId", how="left", validate="one_to_one")
    if actual.quality.isna().any():
        raise ValueError("Launch attribution must cover every eligible launch, including chart misses")
    heat = read_csv(prefix, "artist-heat-attribution").drop(columns=["labelId", "year"])
    sampled = sampled_artist_heat(chart, heat)
    traits = ["quality", "hook", "production", "dance", "initialStock", "initialAwareness", "effectiveRecognition",
              "recognitionRadioLift", "originalCampaignImpact", "spreadCampaignImpact"]
    heat_traits = ["originalHeat", "singlesOnlyHeat", "albumCausedHeatDelta"]
    heat_traits += [key for key in ("currentAlbumsHeat", "retainedAlbumHeatDelta") if key in sampled]
    for year, metrics in annual.items():
        launches = actual[actual.year.eq(year)]
        records = sampled[sampled.year.eq(year) & sampled.currentPosition.le(10)]
        scopes = {"all": None, "big5": BIG5, "head": set(metrics["headLabels"]),
                  "mercury": {"label_0005"}, "rca": {"label_0002"}}
        result = {}
        for scope, labels in scopes.items():
            g = launches if labels is None else launches[launches.labelId.isin(labels)]
            h = records if labels is None else records[records.labelId.isin(labels)]
            result[scope] = dict(launches=len(g), hot100=int(g.hot100.sum()), top10=int(g.top10.sum()),
                                 launchMeans={key: float(g[key].mean()) for key in traits},
                                 top10Quality=float(g.loc[g.top10, "quality"].mean()),
                                 missQuality=float(g.loc[~g.hot100, "quality"].mean()),
                                 sampledTop10Slots=len(h),
                                 slotWeightedHeat={key: float(h[key].mean()) for key in heat_traits},
                                 slotsWithAlbumContributionPct=float(100 * h.albumCausedHeatDelta.gt(1e-6).mean()))
        metrics["attribution"] = result


def analyze(prefix, arm, seed, through=160):
    cols = ["week", "year", "recordId", "artistId", "labelId", "currentPosition", "quality",
            "weeksSinceRelease", "initialLaunchAwareness", "initialLaunchStock", "launchCareerState",
            "launchEffectiveRecognition", "launchRecognitionRadioLift", "genre", "radioHeat"]
    chart = read_csv(prefix, "records", cols)
    chart = chart[chart.currentPosition.between(1, 100) & chart.week.le(through)].copy()
    require_complete_chart(chart, through)
    actual_through = through
    strategy = read_csv(prefix, "release-strategy")
    launches = single_launches(strategy)
    launches["releaseCalendarYear"] = launches.year
    launches["year"] = audit_year(launches.week)
    launches = launches[launches.week.between(1, 156)].copy()
    assert chart.year.eq(audit_year(chart.week)).all()
    fork = read_csv(prefix, "fork-ratios", ["recordId", "qualityEstimate", "reachFactor", "careerBand"])
    assert not fork.recordId.duplicated().any()
    launches = launches.merge(fork, on="recordId", how="left", validate="one_to_one")
    events = read_csv(prefix, "artist-population-events", ["artistId", "cohort"])
    cohort = events.drop_duplicates("artistId").set_index("artistId").cohort
    launches["cohort"] = launches.artistId.map(cohort).fillna("InitialPopulationNoEvent")
    finance = read_csv(prefix, "label-finance", ["week", "year", "labelId", "labelName", "capability", "nationalReach"])
    names = finance.drop_duplicates("labelId").set_index("labelId").labelName
    first = chart.sort_values("week").drop_duplicates("recordId").set_index("recordId")
    launch_metrics = first[["quality", "initialLaunchAwareness", "initialLaunchStock", "launchEffectiveRecognition",
                            "launchRecognitionRadioLift"]]
    launches = launches.join(launch_metrics, on="singleId", validate="one_to_one")
    matching = launches.join(first[["week", "weeksSinceRelease"]].rename(columns={"week": "firstChartWeek"}), on="singleId")
    inferred = matching.firstChartWeek - matching.weeksSinceRelease + 1
    assert inferred[matching.firstChartWeek.notna()].eq(matching.loc[matching.firstChartWeek.notna(), "week"]).all(), prefix
    eligible = conversion_cohorts(launches, chart, actual_through)
    eligible["arm"], eligible["seed"] = arm, seed
    top = chart[chart.currentPosition.le(10)]
    first_top_week = top.groupby("recordId").week.min()
    annual, firms, artists = {}, [], []
    for year in (1960, 1961, 1962):
        t = top[top.year.eq(year)]
        if t.week.nunique() != 52 or len(t) != 520:
            continue
        assert t.groupby("week").currentPosition.nunique().eq(10).all()
        slots = t.groupby("labelId").size().sort_values(ascending=False)
        leaders = list(slots.head(4).index)
        per = t.groupby(["labelId", "recordId"]).size().rename("slots").reset_index()
        head = per[per.labelId.isin(leaders)]
        yr = eligible[eligible.year.eq(year)]
        annual[year] = dict(cr4=float(100 * slots.head(4).sum() / 520), cr8=float(100 * slots.head(8).sum() / 520),
                            headLabels=leaders, headRecords=len(head), headWeeks=float(head.slots.mean()),
                            distinctVisitors=int(t.recordId.nunique()),
                            headCohort=summarize(yr[yr.labelId.isin(leaders)]),
                            big5Cohort=summarize(yr[yr.labelId.isin(BIG5)]))
        fg = summarize_groups(yr, ["labelId"])
        fg = fg.join(t.groupby("labelId").agg(annualVisitors=("recordId", "nunique"), slots=("week", "size")), how="outer")
        fg = fg.join(launches[launches.year.eq(year)].groupby("labelId").size().rename("annualLaunches"), how="outer")
        carry = t[t.recordId.map(first_top_week).lt(1 + (year - 1960) * 52)]
        fg = fg.join(carry.groupby("labelId").agg(carryVisitors=("recordId", "nunique"), carrySlots=("week", "size")))
        count_columns = ["launches", "hot100", "top10", "annualVisitors", "slots", "annualLaunches", "carryVisitors", "carrySlots"]
        fg[count_columns] = fg[count_columns].fillna(0).astype(int)
        fg["arm"], fg["seed"], fg["year"] = arm, seed, year
        fg["head"], fg["big5"] = fg.index.isin(leaders), fg.index.isin(BIG5)
        fg["name"] = fg.index.to_series().map(names).fillna(fg.index.to_series())
        firms.extend(fg.reset_index().to_dict("records"))
        ag = summarize_groups(yr, ["labelId", "artistId"])
        ag = ag.join(t.groupby(["labelId", "artistId"]).agg(annualVisitors=("recordId", "nunique"), slots=("week", "size")), how="outer")
        ag = ag.join(yr.groupby(["labelId", "artistId"]).cohort.first())
        ag["arm"], ag["seed"], ag["year"] = arm, seed, year
        artists.extend(ag.reset_index().to_dict("records"))
    add_attribution(prefix, eligible, chart, annual)
    return dict(prefix=prefix, arm=arm, seed=seed, observedThrough=actual_through, annual=annual), eligible, firms, artists


def inputs(include_new_controls):
    for seed in SEEDS:
        if seed < 4000:
            yield "defaults", seed, f"SimLogs/top10-fiveyear-review-v1-default-{seed}-260"
            yield "inertia040", seed, f"SimLogs/inertia040-fiveyear-v1-default-{seed}-260"
        elif include_new_controls:
            yield "defaults", seed, f"SimLogs/head-hit-default-controls-v1-default-{seed}-168"
        for spread, tag in (("090", "inertia060-push090-fiveyear-confirm-v1"),
                            ("0825", "inertia060-push0825-fiveyear-confirm-v1")):
            yield f"inertia060-push{spread}", seed, f"SimLogs/{tag}-default-{seed}-260"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--include-new-controls", action="store_true")
    parser.add_argument("--through-week", type=int, default=160)
    parser.add_argument("--run", nargs=3, action="append", metavar=("ARM", "SEED", "PREFIX"),
                        help="Analyze these explicit runs instead of the investigation's existing controls")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    runs, cohorts, firms, artists = [], [], [], []
    sources = ((arm, int(seed), prefix) for arm, seed, prefix in args.run) if args.run else inputs(args.include_new_controls)
    for arm, seed, prefix in sources:
        run, c, f, a = analyze(prefix, arm, seed, args.through_week)
        runs.append(run); cohorts.append(c); firms.extend(f); artists.extend(a)
    all_firms, all_artists, launches = pd.DataFrame(firms), pd.DataFrame(artists), pd.concat(cohorts, ignore_index=True)
    focus = all_firms[all_firms.labelId.isin(BIG5)]
    standardized = []
    if "defaults" in set(launches.arm):
        for arm in sorted(set(launches.arm) - {"defaults"}):
            shared_seeds = set(launches.loc[launches.arm.eq("defaults"), "seed"]) & set(launches.loc[launches.arm.eq(arm), "seed"])
            g = launches[launches.seed.isin(shared_seeds) & launches.year.eq(1962) & launches.labelId.isin(BIG5)]
            standardized.append(dict(control="defaults", candidate=arm, year=1962, seeds=sorted(shared_seeds),
                                     comparison=quality_standardization(g[g.arm.eq("defaults")], g[g.arm.eq(arm)])))
    decompositions = []
    for (arm, seed, label), g in focus.groupby(["arm", "seed", "labelId"]):
        y = g.set_index("year")
        if 1960 in y.index and 1962 in y.index:
            decompositions.append(dict(arm=arm, seed=int(seed), labelId=label, name=y.iloc[0]["name"],
                                        change1960to1962=decompose(y.loc[1960], y.loc[1962])))
    path = Path(args.output); path.parent.mkdir(parents=True, exist_ok=True)
    all_firms.sort_values(["arm", "seed", "year", "labelId"]).to_csv(path.with_suffix(".firms.csv"), index=False)
    all_artists.sort_values(["arm", "seed", "year", "labelId", "artistId"]).to_csv(path.with_suffix(".artists.csv"), index=False)
    launches.sort_values(["arm", "seed", "week", "singleId"]).to_csv(path.with_suffix(".cohorts.csv"), index=False)
    result = dict(conversionWindowWeeks=8, releaseWeeksWithinYear=[1, 48], throughWeek=args.through_week,
                  limitations=["Annual Top 10 visitors and release-cohort hits are different measures.",
                               "Cohort year follows 52 audit weeks; releaseCalendarYear retains the event's date year at boundaries.",
                               "Launch quality estimates cover all launches; actual quality and launch traits cover chart entrants only.",
                               "Cohort denotes initial/reserve/runtime population, not proof of an opening-label contract.",
                               "Accounting decompositions are descriptive, not causal estimates.",
                               "Heat attribution, where present, weights only sampled Top 10 slot observations (every four weeks).",
                               "Original and singles-only heat are decompositions in the observed run, not outcomes from a second simulation.",
                               "Stopped runs have complete annual charts but no final lifecycle/economic acceptance review."],
                  runs=runs, big5Decompositions=decompositions, qualityStandardization=standardized)
    path.write_text(json.dumps(result, indent=2, default=lambda v: None if pd.isna(v) else float(v)) + "\n", encoding="utf-8")
    for arm in launches.arm.unique():
        print("\n", arm)
        for year in (1960, 1961, 1962):
            g = launches[launches.arm.eq(arm) & launches.year.eq(year) & launches.labelId.isin(BIG5)]
            print(year, summarize(g))
    print("\nFocus label: Mercury, seed 5005")
    print(focus[focus.seed.eq(5005) & focus.labelId.eq("label_0005")][
        ["arm", "year", "annualLaunches", "annualVisitors", "carryVisitors", "launches", "hot100", "top10",
         "entryRate", "conversionGivenEntry", "estimatedQuality", "promoPct", "starPlusPct", "laterPopulationPct"]].to_string(index=False))


if __name__ == "__main__":
    main()
