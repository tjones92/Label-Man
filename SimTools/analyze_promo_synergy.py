"""Audit A5: promo-single album credit against realized linked-LP outcomes.

Reads the `--promo-synergy-feedback` telemetry of a run (promo-synergy-decisions/outcomes),
checks its identities, and summarizes credited against realized album-side dollars by career
cohort. With --control, also compares strategy mix and chart breadth against an off run.

    python -I SimTools/analyze_promo_synergy.py SimLogs/a5on-1001-104 --control SimLogs/a5off-1001-104
"""
import argparse
import csv
import json
import math
import sys
from collections import Counter, defaultdict

BANDS = ["New/Unsigned", "Rising", "Established", "Star/Superstar", "Fallback"]


def rows(prefix, name):
    with open(f"{prefix}-{name}.csv", newline="", encoding="utf-8") as handle:
        yield from csv.DictReader(handle)


def check_outcomes(prefix):
    """Identity checks, then per-cohort sums of credited and realized album-side dollars."""
    errors, seen = [], set()
    by_band = defaultdict(lambda: Counter())
    for r in rows(prefix, "promo-synergy-outcomes"):
        pid = r["projectId"]
        if pid in seen:
            errors.append(f"duplicate final outcome {pid}")
        seen.add(pid)
        raw, shadow = float(r["rawDemand"]), float(r["shadowDemand"])
        recruited, suppressed = float(r["recruitedUnits"]), float(r["suppressedUnits"])
        margin = float(r["albumMarginPerUnit"])
        if not math.isclose(recruited, max(0.0, raw - shadow), rel_tol=1e-9, abs_tol=1e-6):
            errors.append(f"{pid}: recruited {recruited} != raw - shadow {raw - shadow}")
        if shadow > raw * (1 + 1e-6) + 1e-6:
            errors.append(f"{pid}: counterfactual demand exceeds actual raw demand")
        if suppressed < 0 or recruited < 0:
            errors.append(f"{pid}: negative realized units")
        if float(r["awarenessBonus"]) == 0 and recruited > 1e-6 * max(1.0, raw):
            errors.append(f"{pid}: recruitment without a launch bonus")
        b = by_band[int(r["careerBand"])]
        b["n"] += 1
        b["charted_promo"] += 1 if float(r["awarenessBonus"]) > 0 else 0
        b["credited"] += float(r["creditedRecruitment"])
        b["realized"] += recruited * margin
        b["modeled_cannib"] += float(r["modeledCannibalization"])
        b["realized_cannib"] += suppressed * margin
    return errors, seen, by_band


def decision_summary(prefix, finalized):
    errors, by_period = [], defaultdict(lambda: Counter())
    decided = set()
    for r in rows(prefix, "promo-synergy-decisions"):
        decided.add(r["projectId"])
        rs, cs = float(r["recruitmentScale"]), float(r["cannibalizationScale"])
        if not (0 <= rs <= 2 and 0 <= cs <= 2):
            errors.append(f"{r['projectId']}: scale outside [0, 2]")
        week = int(r["week"])
        p = by_period[week // 13]
        credit = float(r["expectedPromoLift"]) + float(r["promoSynergyGain"])
        p["n"] += 1
        p["credit"] += credit
        p["scaled_credit"] += credit * rs
        p["cannib"] += float(r["cannibalizationLoss"])
        p["scaled_cannib"] += float(r["cannibalizationLoss"]) * cs
        p["weight"] += float(r["evidenceWeight"])
    missing = sorted(finalized - decided)
    if missing:
        errors.append(f"{len(missing)} finalized outcomes have no decision row, e.g. {missing[:3]}")
    return errors, by_period


def strategy_mix(prefix):
    mix = defaultdict(Counter)
    for r in rows(prefix, "release-strategy"):
        mix[int(r["year"])][r["strategy"] or r["chosenFormat"]] += 1
    return mix


def breadth(prefix):
    return {int(r["year"]): r for r in rows(prefix, "chart-breadth")}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("run")
    parser.add_argument("--control")
    parser.add_argument("--output")
    args = parser.parse_args()

    errors, finalized, by_band = check_outcomes(args.run)
    decision_errors, by_period = decision_summary(args.run, finalized)
    errors += decision_errors
    report = {"run": args.run, "finalizedOutcomes": len(finalized), "errors": errors, "bands": {}, "quarters": {}}

    print(f"{args.run}: {len(finalized)} finalized linked LPs, {len(errors)} identity errors")
    print("Cohort            n   promo charted  credited recruit  realized  ratio | modelled divert  realized  ratio")
    for band in sorted(by_band):
        b = by_band[band]
        rr = b["realized"] / b["credited"] if b["credited"] else float("nan")
        rc = b["realized_cannib"] / b["modeled_cannib"] if b["modeled_cannib"] else float("nan")
        report["bands"][BANDS[band]] = {k: b[k] for k in b} | {"recruitmentRatio": rr, "diversionRatio": rc}
        print(f"{BANDS[band]:<15} {b['n']:>5} {b['charted_promo'] / b['n']:>8.1%} {b['credited'] / b['n']:>14,.0f} "
              f"{b['realized'] / b['n']:>9,.0f} {rr:>6.3f} | {b['modeled_cannib'] / b['n']:>14,.0f} "
              f"{b['realized_cannib'] / b['n']:>9,.0f} {rc:>6.3f}")
    print("Quarter  promo projects  mean credit  after scale  mean divert  after scale  mean evidence weight")
    for q in sorted(by_period):
        p = by_period[q]
        report["quarters"][q] = dict(p)
        print(f"{1960 + q // 4}Q{q % 4 + 1} {p['n']:>12} {p['credit'] / p['n']:>12,.0f} {p['scaled_credit'] / p['n']:>12,.0f} "
              f"{p['cannib'] / p['n']:>12,.0f} {p['scaled_cannib'] / p['n']:>12,.0f} {p['weight'] / p['n']:>12.1f}")

    if args.control:
        on, off = strategy_mix(args.run), strategy_mix(args.control)
        on_b, off_b = breadth(args.run), breadth(args.control)
        print("Year  strategy (off / on): OrphanSingle, AlbumWithPromo, AlbumStandalone | Hot100 / Top40 / album-chart acts")
        report["control"] = args.control
        report["years"] = {}
        for year in sorted(set(on) | set(off)):
            cells = [f"{off[year][s]:,}/{on[year][s]:,}" for s in ("OrphanSingle", "AlbumWithPromo", "AlbumStandalone")]
            bcells = [f"{off_b.get(year, {}).get(k, '-')}/{on_b.get(year, {}).get(k, '-')}" for k in ("hot100Acts", "top40Acts", "albumChartActs")]
            report["years"][year] = {"off": dict(off[year]), "on": dict(on[year]), "breadthOff": off_b.get(year), "breadthOn": on_b.get(year)}
            print(f"{year}  {'  '.join(cells)} | {'  '.join(bcells)}")

    for e in errors[:20]:
        print("ERROR", e)
    if args.output:
        with open(args.output, "w", encoding="utf-8") as handle:
            json.dump(report, handle, indent=2, default=str)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
