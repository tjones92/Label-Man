"""Stream exact quarterly promotion evidence without loading a simulation world.

Usage: python analyze_mid_tier_promotions.py SimLogs/RUN [SimLogs/RUN ...] --output SimLogs/REPORT.json
Runs need --mid-tier-promotion-audit. The report verifies that eligible decisions match actual promotions,
and counts both the first blocker and all failed conditions, so overlapping failures stay visible.
Thresholds below describe LabelLifecycleManager.IsIndependentReadyForMidTier; the CSV's eligible column
is the actual game's decision. A disagreement is reported as a failure, never silently recalibrated.
Uses only the Python standard library and reads one CSV row at a time.
"""

import argparse
import csv
import json
import struct
from collections import Counter
from pathlib import Path


def rows(path):
    with path.open(newline="", encoding="utf-8-sig") as stream:
        yield from csv.DictReader(stream)


def failures(row):
    # The gate uses C# floats; round-trip decimal CSV strings must be read back as float32.
    def number(value):
        return struct.unpack("<f", struct.pack("<f", float(value)))[0]

    charting = int(row["recentChartingRecords"])
    roster = int(row["rosterSize"])
    organic = (number(row["ownedReach"]) >= number(.60) and number(row["distributionDependency"]) < number(.35)
               and charting >= 5)
    dependent = charting >= 5 and roster >= 8
    conditions = {
        "OperatingMonths": int(row["monthsActive"]) > 18,
        "CapabilityStreak": int(row["sustainedCapabilityQuarters"]) >= 4,
        "RosterMinimum": roster >= 6,
        "ChartingFloor": charting >= 4,
        "FootprintRoute": organic or dependent,
        "Status": row["status"] in ("Stable", "Rising"),
        "ConsecutiveLosses": int(row["consecutiveLossMonths"]) == 0,
        "Profit": number(row["lastMonthlyProfit"]) > 0,
        "Runway": number(row["cashReserves"]) >= number(6 * number(row["monthlyOverhead"])),
    }
    return [name for name, passed in conditions.items() if not passed]


def summarize(prefix):
    first, all_failed, near_first = Counter(), Counter(), Counter()
    eligible, decisions, mismatches = set(), set(), []
    candidate_labels = set()
    for row in rows(Path(f"{prefix}-mid-tier-promotion-gates.csv")):
        key = (int(row["week"]), row["labelId"])
        if key in decisions:
            raise ValueError(f"Duplicate quarterly decision: {prefix} {key}")
        decisions.add(key)
        failed = failures(row)
        if row["eligible"].lower() not in ("true", "false"):
            raise ValueError(f"Invalid eligibility value: {prefix} {key}")
        actual = row["eligible"].lower() == "true"
        if actual:
            eligible.add(key)
        if actual != (not failed):
            mismatches.append({"week": key[0], "labelId": key[1], "eligible": actual, "failed": failed})
        first[failed[0] if failed else "Eligible"] += 1
        all_failed.update(failed)
        if int(row["recentChartingRecords"]) >= 5:
            candidate_labels.add(row["labelId"])
            near_first[failed[0] if failed else "Eligible"] += 1
    transitions = {
        (int(row["week"]), row["labelId"])
        for row in rows(Path(f"{prefix}-label-tier-transitions.csv"))
        if row["direction"] == "promotion" and row["fromTier"] == "Independent" and row["toTier"] == "MidTier"
    }
    missing = sorted(eligible - transitions)
    unexplained = sorted(transitions - eligible)
    return {
        "run": str(prefix), "decisions": len(decisions), "promotions": len(transitions),
        "firstBlocker": dict(sorted(first.items())), "allFailures": dict(sorted(all_failed.items())),
        "chartingAtLeast5": {"labels": len(candidate_labels), "firstBlocker": dict(sorted(near_first.items()))},
        "decisionMismatches": mismatches, "eligibleWithoutPromotion": missing,
        "promotionsWithoutEligibleDecision": unexplained,
        "pass": bool(decisions) and not (mismatches or missing or unexplained),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    report = [summarize(prefix) for prefix in args.prefixes]
    encoded = json.dumps(report, indent=2)
    if args.output:
        args.output.write_text(encoded + "\n", encoding="utf-8")
    print(encoded)
    return 0 if all(run["pass"] for run in report) else 1


if __name__ == "__main__":
    raise SystemExit(main())
