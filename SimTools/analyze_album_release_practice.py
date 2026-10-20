"""Validate release-practice chronology/accounting and report genre-specific route denominators.

Accept completed run prefixes (without trailing filenames); generated diagnostics stay in SimLogs.
No simulation parameters are inferred or changed by this script.
"""
import argparse
import json
from pathlib import Path

import pandas as pd


def decision_roll(seed, row):
    """Reconstruct the exact 24-bit draw when the six-decimal CSV rounds a boundary."""
    value = 2166136261
    key = f"{seed}|album-practice-v1|advance|{row.labelId}|{row.artistId}|{row.albumId}|{row.week}"
    for char in key:
        value = ((value ^ ord(char)) * 16777619) & 0xffffffff
    return (value >> 8) / 16777216


def inspect(prefix):
    strategy = pd.read_csv(f"{prefix}-release-strategy.csv", low_memory=False)
    launches = pd.read_csv(f"{prefix}-single-launch-quality.csv")
    projects = pd.read_csv(f"{prefix}-album-projects.csv")
    audit_path = Path(f"{prefix}-album-practice.csv")
    audit = pd.read_csv(audit_path) if audit_path.exists() else None
    if launches.singleId.duplicated().any() or projects.albumRecordId.duplicated().any():
        raise ValueError(f"Duplicate release identity: {prefix}")
    strategy_ids = set(strategy.loc[strategy.strategy.isin(["OrphanSingle", "AlbumExtraction"]), "recordId"])
    strategy_ids.update(strategy.loc[strategy.strategy.eq("AlbumWithPromo"), "promoSingleId"])
    if strategy_ids != set(launches.singleId):
        raise ValueError(f"Launches and strategy ledger disagree: {prefix}")
    counts = strategy.groupby(["year", "genre", "strategy"]).size().unstack(fill_value=0)
    for name in ["OrphanSingle", "AlbumExtraction", "AlbumWithPromo", "AlbumStandalone"]:
        if name not in counts:
            counts[name] = 0
    counts["albumProjects"] = counts.AlbumWithPromo + counts.AlbumStandalone
    counts["allSingleLaunches"] = counts.AlbumWithPromo + counts.OrphanSingle + counts.AlbumExtraction
    counts["advancePctOfLps"] = 100 * counts.AlbumWithPromo / counts.albumProjects.replace(0, float("nan"))
    counts["advancePctOfSingles"] = 100 * counts.AlbumWithPromo / counts.allSingleLaunches.replace(0, float("nan"))
    counts = counts.reset_index()
    counts["run"] = prefix.name
    checks = {"run": prefix.name, "launches": len(launches), "projects": len(projects), "validated": audit is not None}
    if audit is None:
        return counts, checks
    # Earlier reference runs predate the opt-in master audit. Their route counts
    # are comparable, but they cannot provide the new master-identity checks.
    genre_launches = pd.read_csv(f"{prefix}-genre-hook-launches.csv")
    lp = audit[audit.route.isin(["AlbumOnly", "AdvanceAlbumSingle"])].copy()
    lp = lp.merge(projects[["albumRecordId", "strategy", "scheduledWeek", "dropWeek", "promoSingleId"]],
                  left_on="albumId", right_on="albumRecordId", how="inner", validate="one_to_one")
    if len(lp) != len(projects):
        raise ValueError(f"Every committed LP needs a practice decision, including week one: {prefix}")
    if not (lp.route.eq("AdvanceAlbumSingle") == lp.strategy.eq("AlbumWithPromo")).all():
        raise ValueError("Campaign decision does not match committed strategy")
    approved = lp[lp.route.eq("AdvanceAlbumSingle")]
    if not (approved.incrementalNet > 0).all() or not approved["format"].isin(["Standard", "Concept"]).all():
        raise ValueError("Advance campaign lacks eligible format or positive incremental economics")
    rounded_boundaries = approved[approved["roll"] >= approved.commissionProbability]
    boundary_evidence = []
    seed = int(prefix.name.rsplit("-", 2)[1])
    for row in rounded_boundaries.itertuples():
        exact = decision_roll(seed, row)
        if abs(exact - row.roll) > .00000051 or exact >= row.commissionProbability:
            raise ValueError("Uncommissioned advance campaign")
        boundary_evidence.append(dict(albumId=row.albumId, exactRoll=exact,
                                      recordedRoll=row.roll, commissionProbability=row.commissionProbability))
    if not (approved.dropWeek - approved.scheduledWeek).between(3, 5).all():
        raise ValueError("Advance single is not ahead of its planned LP")
    if "opportunityCost" in approved:
        recomputed = (approved.singleContribution - approved.preparationCost - approved.campaignCost + approved.lpLift -
                      approved.cannibalization - approved.delayCost - approved.opportunityCost)
        if not (recomputed - approved.incrementalNet).abs().le(.1).all():
            raise ValueError("Advance forecast does not reconcile to its components")
    forbidden = launches.genre.isin(["Classical", "Comedy", "Childrens"]) & launches.strategy.isin(["AlbumWithPromo", "AlbumExtraction"])
    if forbidden.any():
        raise ValueError("Generic specialist programme generated a commercial LP cut")
    extraction = audit[audit.route.eq("PostAlbumSingle")]
    if extraction.albumId.duplicated().any() or not extraction.sourceAlbumAge.between(8, 52).all() or not extraction.sourceAlbumPeak.between(1, 40).all():
        raise ValueError("Extraction repeats, anticipates LP response, or lacks observed LP success")
    if not (extraction.incrementalNet > 0).all():
        raise ValueError("Extraction lacks positive economics")
    # Compare before-extraction source traits to the actually launched master.
    if "masterId" in genre_launches:
        for source, id_column in [(extraction, "singleId"), (approved, "promoSingleId")]:
            actual = source.merge(genre_launches, left_on=id_column, right_on="recordId",
                                  suffixes=("_source", "_launch"), validate="one_to_one")
            if len(actual) != len(source):
                raise ValueError("Recorded cut was not launched")
            for column in ["masterId", "songId"]:
                if actual[f"{column}_source"].isna().any() or not actual[f"{column}_source"].eq(actual[f"{column}_launch"]).all():
                    raise ValueError(f"LP cut changed or lost {column}")
            for column in ["hook", "production"]:
                if not (actual[f"{column}_source"] - actual[f"{column}_launch"]).abs().le(.000001).all():
                    raise ValueError(f"LP cut re-realized {column}")
            if not actual.sourceAlbumRecordId.eq(actual.albumId).all():
                raise ValueError("LP cut lost source LP provenance")
    checks.update(advance=len(approved), laterExtractions=len(extraction), firstDecisionWeek=int(lp.week.min()),
                  reconstructedRoundingBoundaries=boundary_evidence,
                  committedLpAdvancePct=100 * len(approved) / len(lp), passed=True)
    return counts, checks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    counts, checks = zip(*(inspect(prefix) for prefix in args.prefixes))
    output = pd.concat(counts, ignore_index=True)
    output.to_csv(str(args.output) + "-routes.csv", index=False)
    Path(str(args.output) + "-checks.json").write_text(json.dumps(checks, indent=2), encoding="utf-8")
    print(json.dumps(checks, indent=2))
    selected = ["TraditionalPop", "TeenPop", "RockAndRoll", "Soul", "Country", "Jazz", "Classical"]
    print(output[output.genre.isin(selected)][["run", "year", "genre", "albumProjects", "AlbumWithPromo", "AlbumExtraction", "OrphanSingle", "advancePctOfLps", "advancePctOfSingles"]].round(3).to_string(index=False))


if __name__ == "__main__":
    main()
