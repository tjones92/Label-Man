"""Check immutable A3 career tags and summarize the observed revision groups using streaming CSV input.

Usage: python analyze_career_format_memory.py SimLogs/RUN [SimLogs/RUN ...] --output SimLogs/REPORT.json
Counts describe observed releases, not all rosters or decisions. Residual medians describe the distribution
of observations; they are not expected-profit estimates or claims about the live label-level correction.
"""

import argparse
import csv
import json
import math
from collections import defaultdict
from pathlib import Path
from statistics import median

BANDS = ("New/Unsigned", "Rising", "Established", "Star/Superstar", "Other legacy fallback")


def summarize(prefix):
    latest, context = {}, {}
    revisions = 0
    path = Path(f"{prefix}-format-memory-career-revisions.csv")
    with path.open(encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            revisions += 1
            band = int(row["careerBand"])
            if not 0 <= band < len(BANDS):
                raise ValueError(f"Unknown career band: {prefix} {row}")
            immutable_key = row["releaseId"], row["estimatorLane"]
            immutable_value = band, int(row["releaseWeek"])
            if immutable_key in context and context[immutable_key] != immutable_value:
                raise ValueError(f"Release career context changed between revisions: {prefix} {immutable_key}")
            context[immutable_key] = immutable_value
            residual, maturity = float(row["normalizedResidual"]), float(row["maturityWeight"])
            if not (math.isfinite(residual) and .05 <= maturity <= 1 and int(row["releaseWeek"]) <= int(row["chartWeek"])):
                raise ValueError(f"Invalid memory evidence: {prefix} {row}")
            latest[row["labelId"], *immutable_key] = (band, row["revisionKind"] == "Final", residual)
    groups = defaultdict(list)
    labels = defaultdict(set)
    for (label, _, lane), observation in latest.items():
        band, finalized, residual = observation
        groups[lane, band].append((finalized, residual))
        labels[lane, band].add(label)
    return {"run": str(prefix), "revisions": revisions,
            "observedReleaseIds": len({release for release, _ in context}), "observedReleaseLanePairs": len(context), "groups": [
        {"lane": lane, "careerBand": BANDS[band], "labelGroupsWithEvidence": len(labels[lane, band]),
         "releaseObservations": len(values), "finalized": sum(finalized for finalized, _ in values),
         "medianLatestResidual": median(residual for _, residual in values)}
        for (lane, band), values in sorted(groups.items())
    ]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", type=Path, nargs="+")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    result = [summarize(prefix) for prefix in args.prefixes]
    encoded = json.dumps(result, indent=2)
    if args.output:
        args.output.write_text(encoded + "\n", encoding="utf-8")
    print(encoded)


if __name__ == "__main__":
    main()
