"""Compare a combined Top 10 balance probe with defaults and inertia alone.

Reads completed launcher manifests and audit logs only. Reuses the historical
review's annual, separately ranked CR4/CR8 shares and the existing chart guards.
Each configuration must have the same distinct seeds and complete 52-week years.
Usage: python SimTools/analyze_top10_balance_probe.py --candidate SimLogs/TAG/runs.json
       --output SimLogs/TAG/comparison.json
"""
import argparse
import json
from io import BytesIO
from pathlib import Path
from statistics import mean

import pandas as pd

from analyze_label_tier_historical_benchmarks import analyze, review, review_bands
from analyze_rotation_ceiling_probe import run_metrics
from analyze_top10_1961_lag import head_split, history


GUARDS = ("no1Records", "no1Tenure", "no1Spell", "no1ReturnsPct", "closedChartLife",
          "hot100EntriesPerWeek", "top40EntriesPerWeek", "singleUnits",
          "top10SlotsHeldByInertiaPct", "recordsDeniedTop10", "uncensoredWeeksPerRecord",
          "entries8to10GoneNextWeekPct", "big5UpperWeeks", "othersUpperWeeks")


def number_one_metrics(top):
    one = top[top.currentPosition.eq(1)].sort_values("week")
    spells = one.groupby("recordId").week.apply(lambda w: 1 + int((w.diff() > 1).sum()))
    return dict(no1Records=len(spells), no1Tenure=len(one) / len(spells),
                no1Spell=len(one) / int(spells.sum()), no1ReturnsPct=float(100 * (spells > 1).mean()))


def screen_snapshot(manifest_path, seeds):
    """Completed chart years only, including running/stopped audits. Never a formal run review."""
    manifest_path = Path(manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError("Snapshot screening needs explicit distinct --seeds")
    runs, tops = [], []
    for seed in seeds:
        prefix = manifest_path.parent.parent / f"{manifest_path.parent.name}-default-{seed}-{manifest['weeks']}"
        # Freeze bytes and omit the last unfinished CSV line while the writer is active.
        raw = Path(str(prefix) + "-records.csv").read_bytes()
        chart = pd.read_csv(BytesIO(raw[:raw.rfind(b"\n") + 1]),
                            usecols=["week", "year", "recordId", "labelId", "currentPosition"])
        top = chart[chart.currentPosition.between(1, 10)]
        tops.append(top)
        annual = {}
        for year, t in top.groupby("year"):
            if t.week.nunique() != 52 or len(t) != 520:
                continue
            assert not t.duplicated(["week", "recordId"]).any()
            assert t.groupby("week").currentPosition.nunique().eq(10).all()
            slots = t.groupby("labelId").size().sort_values(ascending=False)
            annual[int(year)] = dict(cr4Top10SlotsPct=float(100 * slots.head(4).sum() / 520),
                                     cr8Top10SlotsPct=float(100 * slots.head(8).sum() / 520),
                                     distinctRecords=int(t.recordId.nunique()))
        runs.append(dict(seed=seed, prefix=str(prefix), observedThroughWeek=int(chart.week.max()), annual=annual))
    years = sorted(set.intersection(*(set(r["annual"]) for r in runs)))
    annual_means = {}
    for year in years:
        bands = review_bands(year)
        metrics = {key: dict(meanPct=mean(r["annual"][year][key] for r in runs), bandPct=list(band))
                   for key, band in bands.items()}
        within = all(m["bandPct"][0] <= m["meanPct"] <= m["bandPct"][1] for m in metrics.values())
        annual_means[year] = dict(status="WITHIN_SCREEN_BANDS" if within else "OUTSIDE_SCREEN_BANDS",
                                 metrics=metrics, distinctRecords=mean(r["annual"][year]["distinctRecords"] for r in runs))
    no1 = None
    if years:
        per_seed = [dict(seed=seed, **number_one_metrics(t[t.year.isin(years)])) for seed, t in zip(seeds, tops)]
        no1 = dict(throughYear=max(years), perSeed=per_seed,
                   means={key: mean(r[key] for r in per_seed) for key in per_seed[0] if key != "seed"})
    return dict(kind="COMPLETED_CHART_YEARS_ONLY", formalRunReview=False,
                limitation="Running/stopped audits lack final guard outputs; only complete annual chart slots are screened.",
                manifest=str(manifest_path), runs=runs, annualMeans=annual_means, no1ThroughCompleteYears=no1)


def load_configuration(manifest_path):
    manifest_path = Path(manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    weeks = manifest.get("weeks", 0)
    if not 52 <= weeks <= 260 or weeks % 52:
        raise ValueError("This comparison requires one to five complete 52-week years")
    years = range(1960, 1960 + weeks // 52)
    runs, seeds = [], []
    for result in manifest["results"]:
        if result.get("passed") is not True or result.get("exitCode") != 0:
            raise ValueError(f"{result['name']}: incomplete or stopped run cannot enter a formal comparison")
        seed = int(next(a.split("=", 1)[1] for a in result["args"] if a.startswith("--seed=")))
        prefix = str(manifest_path.parent.parent / result["name"])
        annual = analyze(prefix)
        chart = pd.read_csv(prefix + "-records.csv", usecols=["year", "recordId", "labelId", "currentPosition"])
        top = chart[chart.currentPosition.between(1, 10)]
        splits = {year: head_split(t.groupby(["labelId", "recordId"]).size().rename("slots")
                                  .reset_index().rename(columns={"labelId": "firm"}))
                  for year, t in top.groupby("year")}
        for year, t in top.groupby("year"):
            splits[year]["big5SlotSharePct"] = 100 * t.labelId.isin({f"label_000{i}" for i in range(1, 6)}).mean()
        guards = run_metrics(prefix)
        # Include every genre in paired checks, including those outside the ten
        # largest shares printed by the older single-probe report.
        shape = pd.read_csv(prefix + "-genre-decade-shape.csv")
        slots_by_genre = shape.groupby("genre").top10RecordWeeks.sum()
        guards["top10GenreSlotPct"] = (100 * slots_by_genre / slots_by_genre.sum()).to_dict()
        runs.append(dict(seed=seed, prefix=prefix, annual=annual["annual"],
                         guards=guards, duration={"annual": splits}))
        seeds.append(seed)
    # The existing reviewer checks completion, seed uniqueness, assembly identity,
    # flags, duration, and all 520 slots per year. Never pool firms across seeds.
    reviews = {year: review(runs, manifest, year) for year in years}
    summary = {year: {
        "cr4": reviews[year]["metrics"]["cr4Top10SlotsPct"]["meanPct"],
        "cr8": reviews[year]["metrics"]["cr8Top10SlotsPct"]["meanPct"],
        "status": reviews[year]["status"],
        "distinctRecords": mean(r["annual"][year]["distinctTop10Records"] for r in runs),
        "weeksPerRecord": mean(520 / r["annual"][year]["distinctTop10Records"] for r in runs),
        **{key: mean(r["duration"]["annual"][year][key] for r in runs)
           for key in ("headRecords", "headWeeksPerRecord", "restWeeksPerRecord", "big5SlotSharePct")},
        "majorTierSlotPct": mean(r["annual"][year]["top10SlotsByChartWeekTierPct"]["Major"] for r in runs),
    } for year in years}
    return dict(manifest=str(manifest_path), assemblySha256=manifest["assemblySha256"],
                weeks=weeks, seeds=sorted(seeds), runs=runs, reviews=reviews, annualMeans=summary,
                guardMeans={key: mean(r["guards"][key] for r in runs) for key in GUARDS})


def markdown(result):
    groups = result["configurations"]
    years = list(groups["combined"]["annualMeans"])
    labels = [name.capitalize() for name in groups]
    lines = ["# Combined inertia / radio-push balance probe", "",
             "Annual shares are equal-weight seed means; each seed's firms are ranked separately.",
             "The adopted concentration bands are advisory. Turnover uses the reconstructed annual counts.", "",
             "| Year | Historical records | " + " | ".join(f"{s} CR4 / CR8" for s in labels) + " | Combined status |",
             "|---|---:|" + "---:|" * len(groups) + "---|"]
    for year in years:
        rows = [groups[g]["annualMeans"][year] for g in groups]
        cells = [f"{r['cr4']:.2f} / {r['cr8']:.2f}" for r in rows]
        lines.append(f"| {year} | {result['history'][year]['distinctRecords']} | " +
                     " | ".join(cells) + f" | {rows[-1]['status']} |")
    lines += ["", "| Year | " + " | ".join(f"{s} records / weeks" for s in labels) + " | Historical weeks |",
              "|---|" + "---:|" * (len(groups) + 1)]
    for year in years:
        cells = [f"{g['annualMeans'][year]['distinctRecords']:.1f} / {g['annualMeans'][year]['weeksPerRecord']:.2f}"
                 for g in groups.values()]
        lines.append(f"| {year} | " + " | ".join(cells) +
                     f" | {result['history'][year]['allWeeksPerRecord']:.2f} |")
    lines += ["", f"| Guard ({groups['combined']['weeks']} weeks) | " + " | ".join(labels) + " |",
              "|---|" + "---:|" * len(groups)]
    for key in GUARDS:
        cells = [f"{g['guardMeans'][key]:.3f}" for g in groups.values()]
        lines.append(f"| {key} | " + " | ".join(cells) + " |")
    lines += ["", "| Year | Historical head / rest weeks | " + " | ".join(f"{s} head / rest" for s in labels) + " |",
              "|---|" + "---:|" * (len(groups) + 1)]
    for year in years:
        rows = [result["history"][year]] + [g["annualMeans"][year] for g in groups.values()]
        lines.append(f"| {year} | " + " | ".join(
            f"{r['headWeeksPerRecord']:.2f} / {r['restWeeksPerRecord']:.2f}" for r in rows) + " |")
    lines += ["", "Paired changes (combined versus each reference):", ""]
    for label, pairs in result["paired"].items():
        units = [p["singleUnitsPct"] for p in pairs]
        lines.append(f"- {label}: single units {min(units):+.2f}% to {max(units):+.2f}%; "
                     f"largest absolute genre Top 10 slot change {max(p['largestGenreSlotDeltaPp'] for p in pairs):.2f} points.")
    if result["turnoverGapClosedPct"] is not None:
        lines += ["", f"1961-{years[-1]} turnover gap closed: {result['turnoverGapClosedPct']:.1f}%."]
    lines += ["", f"These runs cover 1960-{years[-1]}. Later years remain untested.",
              "A 52-week screen cannot resolve #1 tenure; those guards require multi-year confirmation.",
              "Cross-arm changes reshuffle the RNG stream; paired differences describe full-system outcomes.", ""]
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--control", help="Defaults to the matching 52- or 260-week baseline")
    parser.add_argument("--inertia", help="Optional inertia-only reference; defaults to 17f for 260-week probes")
    parser.add_argument("--candidate", required=True)
    parser.add_argument("--screen", action="store_true", help="Screen complete chart years of a running/stopped audit; no formal run review")
    parser.add_argument("--seeds", nargs="+", type=int, help="Explicit seeds for --screen")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    if args.screen:
        result = screen_snapshot(args.candidate, args.seeds)
        path = Path(args.output)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result["annualMeans"], indent=2))
        if result["no1ThroughCompleteYears"]:
            print("#1 through complete years:", result["no1ThroughCompleteYears"]["means"])
        return
    candidate = load_configuration(args.candidate)
    control = args.control or ("SimLogs/historical-top10-review-v2/runs.json" if candidate["weeks"] == 52
                               else "SimLogs/top10-fiveyear-review-v1/runs.json")
    inertia = args.inertia or ("SimLogs/inertia040-fiveyear-v1/runs.json" if candidate["weeks"] == 260 else None)
    groups = {"defaults": load_configuration(control)}
    if inertia:
        groups["inertia"] = load_configuration(inertia)
    groups["combined"] = candidate
    if len({tuple(g["seeds"]) for g in groups.values()}) != 1:
        raise ValueError("Configurations must use the same seeds")
    if len({g["weeks"] for g in groups.values()}) != 1:
        raise ValueError("Configurations must use the same duration")
    paired = {}
    for reference in (name for name in groups if name != "combined"):
        prior = {r["seed"]: r["guards"] for r in groups[reference]["runs"]}
        pairs = []
        for run in groups["combined"]["runs"]:
            base, probe_metrics = prior[run["seed"]], run["guards"]
            genres = set(base["top10GenreSlotPct"]) | set(probe_metrics["top10GenreSlotPct"])
            delta = {g: probe_metrics["top10GenreSlotPct"].get(g, 0) - base["top10GenreSlotPct"].get(g, 0)
                     for g in genres}
            pairs.append(dict(seed=run["seed"], singleUnitsPct=100 * (probe_metrics["singleUnits"] / base["singleUnits"] - 1),
                              genreSlotDeltaPp=delta, largestGenreSlotDeltaPp=max(abs(v) for v in delta.values()),
                              guardsDelta={k: probe_metrics[k] - base[k] for k in GUARDS}))
        paired[reference] = pairs
    historical = history()
    later_years = [y for y in candidate["annualMeans"] if y > 1960]
    gap_closed = None
    if later_years:
        hist_count = mean(historical[y]["distinctRecords"] for y in later_years)
        base_count = mean(groups["defaults"]["annualMeans"][y]["distinctRecords"] for y in later_years)
        candidate_count = mean(candidate["annualMeans"][y]["distinctRecords"] for y in later_years)
        gap_closed = 100 * (candidate_count - base_count) / (hist_count - base_count)
    result = dict(history=historical, configurations=groups, paired=paired,
                  turnoverGapClosedPct=gap_closed)
    path = Path(args.output)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    report = markdown(result)
    path.with_suffix(".md").write_text(report, encoding="utf-8")
    print(report)


if __name__ == "__main__":
    main()
