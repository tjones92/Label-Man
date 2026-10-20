"""Validate the audit boundary and print the source/format mechanisms it observes.

Usage: python SimTools/validate_genre_hook_audit.py BEFORE_PREFIX AFTER_PREFIX --output SimLogs/check.json
The only permitted extra outputs are the two opt-in audit CSVs.
"""
import argparse
import hashlib
import json
from pathlib import Path

import pandas as pd

EXTRA = {"genre-format-units.csv", "genre-hook-launches.csv"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    def files(prefix):
        return {p.name[len(prefix.name) + 1:]: p for p in prefix.parent.glob(prefix.name + "-*.csv")
                if not p.name.endswith("-performance-profile.csv")}

    before, after = files(args.before), files(args.after)
    if not before or not after:
        raise ValueError("Both runs must have outputs")
    missing = sorted(before.keys() - after.keys())
    unexpected = sorted(after.keys() - before.keys() - EXTRA)
    mismatches = [name for name in sorted(before.keys() & after.keys())
                  if hashlib.sha256(before[name].read_bytes()).digest() != hashlib.sha256(after[name].read_bytes()).digest()]
    result = dict(before=str(args.before), after=str(args.after), compared=len(before.keys() & after.keys()),
                  missing=missing, unexpected=unexpected, mismatches=mismatches)
    if "genre-format-units.csv" in after:
        units = pd.read_csv(after["genre-format-units.csv"])
        shape = pd.read_csv(after["genre-decade-shape.csv"])
        check = units.groupby(["year", "genre"]).units.sum().rename("formatUnits").reset_index().merge(
            shape[["year", "genre", "marketUnits"]], on=["year", "genre"], how="outer").fillna(0)
        result["formatReconciles"] = bool((check.formatUnits == check.marketUnits).all())
        print("Physical units by format:\n" + units.groupby(["year", "format"]).units.sum().to_string())
    if "genre-hook-launches.csv" in after:
        launches = pd.read_csv(after["genre-hook-launches.csv"])
        result["auditLaunches"] = len(launches)
        q = pd.read_csv(after["single-launch-quality.csv"])
        check = launches.merge(q, left_on="recordId", right_on="singleId", suffixes=("", "_quality"), validate="one_to_one")
        result["launchesReconcile"] = len(check) == len(q) == len(launches)
        result["launchQualityReconciles"] = bool((check.hook == check.hook_quality).all()
                                                  and (check.production == check.production_quality).all())
        summary = launches.groupby(["genre", "tier", "strategy"]).agg(
            launches=("recordId", "size"), hook=("hook", "mean"), production=("production", "mean"),
            execution=("execution", "mean"), capability=("capability", "mean"))
        summary.to_csv(args.output.with_name(args.output.stem + "-execution.csv"))
        print("Independent recordings:\n" + summary.xs("Independent", level="tier").round(4).to_string())
        pro = launches[launches.source.isin(["ExternalProfessional", "LabelStaffWriter", "ArtistCowrittenWithProfessional"])]
        usage = pro.groupby(["genre", "songId"]).agg(launches=("recordId", "size"), labels=("labelId", "nunique"))
        usage.to_csv(args.output.with_name(args.output.stem + "-professional-usage.csv"))
        print("Professional catalogue reuse:\n" + usage.groupby("genre").agg(
            songs=("launches", "size"), launches=("launches", "sum"), maxLabelsPerSong=("labels", "max"),
            sharedSongs=("labels", lambda x: int((x > 1).sum()))).to_string())
    result["passed"] = not missing and not unexpected and not mismatches and all(
        result.get(k, True) for k in ("formatReconciles", "launchesReconcile", "launchQualityReconciles"))
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
