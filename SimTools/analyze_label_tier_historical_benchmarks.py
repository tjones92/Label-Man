"""Compare existing audits with historical Top 10 slot concentration.

Reads logs only. Requires pandas. Deliberately rejects absorption runs: the
current audit label identity is an operating business proxy, not a reconstructed
historical parent-company ontology. No simulation or balancing changes.
"""
import argparse
import json
from pathlib import Path
from statistics import mean, stdev

import pandas as pd


# Peterson and Berger (1975), Table 1, p. 160, visually checked against the scan.
# CR4/CR8 are annual weekly Top 10 SLOT shares, not chart-unit shares.
HISTORY = {
    1960: (45, 39, 20, 28, 52),
    1961: (48, 39, 16, 27, 48),
    1962: (52, 41, 21, 25, 46),
    1963: (52, 36, 15, 26, 55),
    1964: (53, 37, 17, 34, 51),
    1965: (50, 35, 16, 37, 61),
    1966: (49, 31, 13, 38, 61),
    1967: (51, 35, 15, 40, 60),
    1968: (46, 30, 17, 42, 61),
    1969: (48, 31, 14, 42, 64),
}
TIERS = ["Major", "MidTier", "Independent", "Boutique", "Small"]
# Billboard weekly Top 10 reconstruction, operating label, slots (HistoricalLabelTierBalanceResearch.md
# sections 12-13; SimLogs/historical-top10-summary.csv): CR4, CR8, firms 5-8, labels with a Top 10 hit.
# P-B's series above consolidates firms and does not reproduce, so only the reconstruction sets bands.
RECONSTRUCTION = {
    1960: (34.6, 52.7, 18.1, 49),
    1961: (27.9, 43.5, 15.6, 51),
    1962: (26.7, 43.3, 16.5, 53),
    1963: (26.3, 41.9, 15.6, 54),
    1964: (29.2, 43.7, 14.4, 55),
    1968: (28.1, 44.0, 16.0, 49),
    1969: (26.0, 43.3, 17.3, 48),
}
# 1961-69 differ by less than one year's bootstrap noise (~4 pts), so they share one pooled band;
# 1965-67 were not reconstructed and use it too. Bands are about +/-5 pts, ~1.2 bootstrap SDs.
POOLED_1961_69 = (27.4, 43.3, 15.9, 52)
REVIEW_BANDS = {
    1960: {"cr4Top10SlotsPct": (30.0, 40.0), "cr8Top10SlotsPct": (48.0, 58.0)},
    "1961-69": {"cr4Top10SlotsPct": (22.0, 32.0), "cr8Top10SlotsPct": (38.0, 48.0)},
}
MINIMUM_REVIEW_SEEDS = 3


def review_bands(year):
    if year == 1960:
        return REVIEW_BANDS[1960]
    if 1961 <= year <= 1969:
        return REVIEW_BANDS["1961-69"]
    raise ValueError(f"No operating-label review band for {year}")


def reference(year):
    cr4, cr8, firms_5_to_8, labels = RECONSTRUCTION.get(year, POOLED_1961_69)
    return {"cr4Top10SlotsPct": cr4, "cr8Top10SlotsPct": cr8, "firms5to8Top10SlotsPct": firms_5_to_8,
            "operatingLabelsWithAnyTop10": labels,
            "basis": "reconstructed year" if year in RECONSTRUCTION else "pooled 1961-69 mean (not reconstructed)"}


def review(runs, manifest, year=1960):
    """Review equal-weight seed means from a single completed configuration.

    Require a launcher manifest so an old control, a duplicate seed, a different
    flag stack or a partial year cannot silently enter the sample.
    These are advisory historical bands, not simulation acceptance gates.
    """
    bands = review_bands(year)
    if not manifest.get("assemblySha256"):
        raise ValueError("Review requires a recorded simulation assembly hash")
    results = manifest.get("results", [])
    names = [result["name"] for result in results]
    if len(names) != len(set(names)):
        raise ValueError("Duplicate run names in review manifest")
    inputs = {Path(run["prefix"]).name: run for run in runs}
    if len(inputs) != len(runs) or set(inputs) != set(names):
        raise ValueError("Review inputs must match the manifest's runs exactly")
    configuration = None
    seeds = set()
    per_seed = []
    for result in results:
        if result.get("passed") is not True or result.get("exitCode") != 0:
            raise ValueError(f"{result['name']}: native audit did not complete successfully")
        args = result["args"]
        seed_args = [arg for arg in args if arg.startswith("--seed=")]
        if len(seed_args) != 1:
            raise ValueError(f"{result['name']}: expected one explicit seed")
        seed = int(seed_args[0].split("=", 1)[1])
        if seed in seeds:
            raise ValueError(f"Duplicate seed {seed}; repeats are not independent samples")
        seeds.add(seed)
        flags = tuple(sorted(arg for arg in args if not arg.startswith(("--seed=", "--run="))))
        if configuration is not None and flags != configuration:
            raise ValueError("Review runs must use the same flags and duration")
        configuration = flags
        annual = inputs[result["name"]]["annual"]
        row = annual.get(year, annual.get(str(year)))
        if row is None or row["chartWeeks"] != 52 or row["top10Slots"] != 520:
            raise ValueError(f"{result['name']}: review requires all 52 weeks of {year}")
        values = {metric: float(row[metric]) for metric in bands}
        if not all(0 <= value <= 100 for value in values.values()):
            raise ValueError("Review shares must be finite percentages in [0, 100]")
        per_seed.append({"seed": seed, "run": result["name"], **values})
    enough = len(per_seed) >= MINIMUM_REVIEW_SEEDS
    metrics = {}
    warnings = []
    for metric, (lower, upper) in bands.items():
        values = [row[metric] for row in per_seed]
        average = mean(values) if values else None
        within = lower <= average <= upper if average is not None else None
        metrics[metric] = {
            "bandPct": [lower, upper], "meanPct": average,
            "sampleStandardDeviationPp": stdev(values) if len(values) >= 2 else None,
            "minimumPct": min(values) if values else None,
            "maximumPct": max(values) if values else None,
            "meanWithinBand": within,
        }
        if enough and not within:
            warnings.append(f"{year} {metric} seed mean {average:.2f}% is outside {lower:g}-{upper:g}%")
    if not enough:
        warnings.append(f"{year} review needs at least {MINIMUM_REVIEW_SEEDS} distinct seeds; found {len(per_seed)}")
    return {
        "year": year, "status": ("INSUFFICIENT_SEEDS" if not enough else
                                  "REVIEW" if warnings else "WITHIN_BANDS"),
        "historicalReference": reference(year),
        "advisoryOnly": True, "seedCount": len(per_seed),
        "minimumSeedCount": MINIMUM_REVIEW_SEEDS,
        "aggregation": "Unweighted mean of each seed's separately ranked annual Top 10 slot shares",
        "assemblySha256": manifest["assemblySha256"],
        "configurationArgsWithoutRunAndSeed": list(configuration or []),
        "perSeed": sorted(per_seed, key=lambda row: row["seed"]),
        "metrics": metrics, "historicalWarnings": warnings,
        "interpretation": ("Bands centered on the Billboard operating-label reconstruction, about +/-5 pts; "
                           "not confidence intervals; simulation operating label vs historical operating label"),
    }


def analyze(prefix):
    ledger = pd.read_csv(prefix + "-deal-ledger.csv", usecols=["resolution"])
    if ledger.resolution.eq("Absorb").any():
        raise ValueError(f"{prefix}: reconstruct corporate owners before comparison")
    cols = ["week", "year", "recordId", "artistId", "labelId", "labelTier",
            "genre", "currentPosition", "unitsThisWeek"]
    chunks = []
    for chunk in pd.read_csv(prefix + "-records.csv", usecols=cols, chunksize=150000):
        chunks.append(chunk[chunk.currentPosition.between(1, 100)].copy())
    records = pd.concat(chunks, ignore_index=True)
    assert not records.duplicated(["week", "recordId"]).any()
    concentration = pd.read_csv(prefix + "-concentration.csv").set_index("year")
    directory = pd.read_csv(prefix + "-label-directory.csv").set_index("labelId")
    # The directory is written at run end, so a label that charted and later exited is missing from it; take its
    # name from the first-chart log instead. Names are display only -- shares are keyed by labelId.
    first_path = Path(prefix + "-first-chart-events.csv")
    first_names = (pd.read_csv(first_path, usecols=["releaseLabelId", "labelName"])
                   .drop_duplicates("releaseLabelId").set_index("releaseLabelId").labelName
                   if first_path.exists() else pd.Series(dtype=object))
    label_name = lambda label_id: directory.labelName.get(label_id, first_names.get(label_id, label_id))
    events = pd.read_csv(prefix + "-artist-population-events.csv", usecols=[
        "week", "eventType", "artistId", "currentPrimaryGenre", "labelTier",
        "priorContractCount"])
    opening_events = events[events.week.le(52)]
    signs = opening_events[opening_events.eventType.isin(["signing", "re-signing"])]
    first_signs = signs[signs.eventType.eq("signing")]
    assert first_signs.priorContractCount.eq(0).all()
    all_signed_ids = set(signs.artistId)
    all_top10 = records[records.currentPosition.le(10)]
    first_top10 = all_top10.sort_values("week").drop_duplicates("recordId")
    annual = {}
    for year, frame in records.groupby("year"):
        total = int(frame.unitsThisWeek.sum())
        assert total == int(concentration.loc[year, "totalChartUnits"])
        top = frame[frame.currentPosition.le(10)]
        assert len(top) > 0
        assert top.groupby("week").size().eq(10).all()
        assert top.groupby("week").currentPosition.nunique().eq(10).all()
        assert top.week.nunique() == frame.week.nunique()
        slots = top.groupby("labelId").size().sort_values(ascending=False)
        new_hits = first_top10[first_top10.year.eq(year)]
        annual[int(year)] = {
            "chartWeeks": int(frame.week.nunique()), "top10Slots": len(top),
            "totalChartUnits": total,
            "cr4Top10SlotsPct": float(100 * slots.head(4).sum() / len(top)),
            "cr8Top10SlotsPct": float(100 * slots.head(8).sum() / len(top)),
            "firms5to8Top10SlotsPct": float(100 * slots.iloc[4:8].sum() / len(top)),
            "operatingLabelsWithAnyTop10": len(slots),
            "operatingLabelsWithNewTop10": int(new_hits.labelId.nunique()),
            "distinctTop10Records": int(top.recordId.nunique()),
            "newTop10Records": len(new_hits),
            "top10SlotsByChartWeekTierPct": {
                tier: float(100 * top.labelTier.eq(tier).sum() / len(top))
                for tier in TIERS},
            "leadingTop10OperatingLabels": [
                {"labelId": label_id, "labelName": label_name(label_id),
                 "slots": int(count), "sharePct": float(100 * count / len(top))}
                for label_id, count in slots.head(8).items()],
        }
    teen = records[records.year.eq(1960) & records.genre.eq("TeenPop")]
    teen_total = int(teen.unitsThisWeek.sum())
    mid = teen[teen.labelTier.eq("MidTier")]
    mid_units = int(mid.unitsThisWeek.sum())
    recent_units = int(mid.loc[mid.artistId.isin(all_signed_ids), "unitsThisWeek"].sum())
    by_label = mid.groupby("labelId").unitsThisWeek.sum().sort_values(ascending=False)
    opening = {
        "teenPopChartUnits": teen_total,
        "teenPopChartUnitsByOperatingTierPct": {
            tier: float(100 * teen.loc[teen.labelTier.eq(tier), "unitsThisWeek"].sum()
                        / teen_total) for tier in TIERS},
        "midTierFirstTimeSigningsAllGenres": int(first_signs.loc[
            first_signs.labelTier.eq("MidTier"), "artistId"].nunique()),
        "midTierFirstTimeSigningsCurrentGenreTeenPop": int(first_signs.loc[
            first_signs.labelTier.eq("MidTier") &
            first_signs.currentPrimaryGenre.eq("TeenPop"), "artistId"].nunique()),
        "midTierTeenPopChartUnits": mid_units,
        "midTierTeenPopUnitsFromArtistsWithAnyOpeningYearSigningEvent": recent_units,
        "midTierTeenPopUnitsFromArtistsWithoutOpeningYearSigningEvent": mid_units - recent_units,
        "midTierTeenPopNoSigningEventUnitSharePct": float(
            100 * (mid_units - recent_units) / mid_units),
        "leadingMidTierTeenPopLabels": [
            {"labelId": label_id, "labelName": label_name(label_id),
             "chartUnits": int(units), "shareOfMidTierTeenPopPct": float(100 * units / mid_units)}
            for label_id, units in by_label.items()],
    }
    return {"prefix": prefix, "absorptions": 0, "annual": annual, "openingYear": opening}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+")
    parser.add_argument("--output", required=True)
    parser.add_argument("--review-manifest",
                        help="Review one matched multi-seed configuration against each complete year's band")
    args = parser.parse_args()
    history = {year: dict(zip([
        "labels", "firms", "firmsWithOneHit", "cr4Top10SlotsPct", "cr8Top10SlotsPct"
    ], values)) for year, values in HISTORY.items()}
    result = {
        "historicalSource": "https://people.uncw.edu/ricej/art/cycles%20in%20symbol%20production.pdf",
        "historicalSourcePages": "160 (table), 170-171 (method)",
        "historical": history,
        "reconstructionOperatingLabel": {year: reference(year) for year in RECONSTRUCTION},
        "reviewBands": {str(key): value for key, value in REVIEW_BANDS.items()},
        "limitations": [
            "Simulation businesses are operating-label proxies; historical firms consolidate imprints/affiliations.",
            "1960 first-observed Top 10 hits cannot exclude 1959 carryover in these logs.",
            "New-hit firm counts exclude later-year carryover; annual slot shares use all observed Top 10 slots.",
            "Genre at signing and genre of a chart record are separate fields.",
            "No-signing-event cohort is based on event logging, not a reconstructed opening roster.",
            "Cross-arm differences are descriptive, not identified causal policy effects.",
        ],
        "runs": [analyze(prefix) for prefix in args.prefixes],
    }
    if args.review_manifest:
        manifest = json.loads(Path(args.review_manifest).read_text(encoding="utf-8-sig"))
        years = sorted(set.intersection(*[
            {year for year, row in run["annual"].items()
             if 1960 <= year <= 1969 and row["chartWeeks"] == 52 and row["top10Slots"] == 520}
            for run in result["runs"]]))
        result["reviews"] = {year: review(result["runs"], manifest, year) for year in years}
    path = Path(args.output)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")

    def marked(year, metric, value):
        lower, upper = review_bands(year)[metric]
        return f"{value:.2f}%" + ("" if lower <= value <= upper else "*")

    for run in result["runs"]:
        print(run["prefix"])
        for year, row in run["annual"].items():
            if not 1960 <= year <= 1969:
                continue
            ref = reference(year)
            print(f"  {year}: CR4 {marked(year, 'cr4Top10SlotsPct', row['cr4Top10SlotsPct'])} "
                  f"(hist {ref['cr4Top10SlotsPct']:g}), "
                  f"CR8 {marked(year, 'cr8Top10SlotsPct', row['cr8Top10SlotsPct'])} "
                  f"(hist {ref['cr8Top10SlotsPct']:g}), "
                  f"5-8 {row['firms5to8Top10SlotsPct']:.2f}% (hist {ref['firms5to8Top10SlotsPct']:g}), "
                  f"{row['operatingLabelsWithAnyTop10']} labels with a Top 10 "
                  f"(hist {ref['operatingLabelsWithAnyTop10']})")
        opening = run["openingYear"]
        print("  MidTier first signings, all / TeenPop:",
              opening["midTierFirstTimeSigningsAllGenres"],
              opening["midTierFirstTimeSigningsCurrentGenreTeenPop"])
    print("  (* = outside that year's operating-label review band)")
    for year, row_review in result.get("reviews", {}).items():
        print(f"{year} multi-seed review: {row_review['status']} ({row_review['seedCount']} seeds)")
        for metric, row in row_review["metrics"].items():
            average = "unavailable" if row["meanPct"] is None else f"{row['meanPct']:.2f}%"
            print(f"  {metric}: {average}; review band {row['bandPct'][0]:g}-{row['bandPct'][1]:g}%")
        for warning in row_review["historicalWarnings"]:
            print("  REVIEW:", warning)


if __name__ == "__main__":
    main()
