"""Gate report for the cohesion / landmark pair, against a paired baseline run.

usage: python cohesion_gate.py <run> --baseline evo12

Answers the questions the SmoothStep + EarlyStatementExcellence + landmark-bar changes are
gated on, in one place. The two changes are mechanically separable in this output, which is why
they can share one A/B:

  * the COHESION pair moves `cohesionCeiling`, the pioneer count, the concept-album count and
    (through pooledAppeal) LP unit share and album-chart composition;
  * the LANDMARK BAR moves landmark counts, legitimacy and the tastemaker set, and touches the
    cohesion ceiling only through legitimacy's bounded lift.

`ArtisticMeritService.GetCraft` reads `max(bodyOfWork, thematicCohesion)` and bodyOfWork wins in
every measured year, so cohesion is inert on the landmark channel -- which is what makes the
attribution above safe rather than merely convenient.

Gate 4's bands, restated:
  * LP unit share 29.5 / 35.0 / 41.3 / 48.4 / 55.4 at 1960/62/64/66/68, tolerance +-1.5 pts
  * landmarks 25-40 across the decade, "a handful before 1965, then three or four a year"
  * concept albums: a handful across 1965-66, a wave by 1968
  * total market units within seed noise
"""

from __future__ import annotations

import argparse
import os
import sys
from collections import defaultdict

import pandas as pd

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import SIMLOGS  # noqa: E402

YEARS = list(range(1960, 1970))
LP_BAND = {1960: 29.5, 1962: 35.0, 1964: 41.3, 1966: 48.4, 1968: 55.4}
LP_TOLERANCE = 1.5
LANDMARK_TARGET = (25, 40)
# The pioneer path lifts the ceiling to this floor, so an album at or above it in a year the
# ordinary ramp cannot reach it is a pioneer. Read from the run rather than assumed.
PIONEER_FLOOR = .80


def read(run: str, name: str) -> pd.DataFrame | None:
    path = os.path.join(SIMLOGS, f"{run}-{name}.csv")
    return pd.read_csv(path) if os.path.exists(path) else None


def albums(run: str) -> pd.DataFrame | None:
    frame = read(run, "album-composition")
    return None if frame is None else frame[frame.albumFormat != "Soundtrack"]


def series(frame: pd.DataFrame | None, values) -> dict:
    return {} if frame is None else values(frame)


def line(label: str, table: dict, width: int = 7, fmt: str = "%7.1f") -> str:
    return label.ljust(22) + "".join((fmt % table[year]) if year in table else " " * width
                                     for year in YEARS)


def report(run: str, baseline: str | None) -> None:
    print(f"=== {run}" + (f"  (baseline {baseline})" if baseline else "") + " ===")
    print("".ljust(22) + "".join("%7d" % year for year in YEARS))

    for name in ([run, baseline] if baseline else [run]):
        composition = albums(name)
        if composition is None:
            print(f"  {name}: no album-composition.csv (a --calibration run suppresses it)")
            continue
        events = read(name, "cultural-events")
        formats = read(name, "format-mix")

        print(f"-- {name}")
        grouped = composition.groupby("year")
        print(line("  albums", grouped.size().to_dict(), fmt="%7d"))
        if "cohesionCeiling" in composition:
            print(line("  cohesionCeiling", grouped.cohesionCeiling.mean().to_dict(), fmt="%7.3f"))
            pioneers = composition[composition.cohesionCeiling >= PIONEER_FLOOR]
            print(line("  pioneers(>=.80)", pioneers.groupby("year").size().to_dict(), fmt="%7d"))
        print(line("  thematicCohesion", grouped.thematicCohesion.mean().to_dict(), fmt="%7.3f"))
        concept = composition[composition.albumFormat == "Concept"]
        print(line("  concept albums", concept.groupby("year").size().to_dict(), fmt="%7d"))

        if events is not None:
            landmarks = events[events.eventType == "LandmarkAlbum"]
            counts = landmarks.groupby("year").size().to_dict()
            print(line("  landmarks", counts, fmt="%7d"))
            total = int(sum(counts.values()))
            low, high = LANDMARK_TARGET
            verdict = "PASS" if low <= total <= high else "**FAIL**"
            print(f"     decade total {total}  target {low}-{high}  {verdict}   "
                  f"early(60-64) {sum(counts.get(y, 0) for y in YEARS[:5])} "
                  f"late(65-69) {sum(counts.get(y, 0) for y in YEARS[5:])}   "
                  f"tastemakers {landmarks.artistId.nunique()}   "
                  f"end legitimacy {landmarks.legitimacy.max():.4f}")

        if formats is not None:
            # period == "annual" only. format-mix carries a weekly row per format as well, and
            # summing both counts every unit twice over -- which reads as a 12-point LP miss.
            annual = formats[formats.period == "annual"]
            share = {int(year): 100 * rows[rows.releaseFormat == "Album"].units.sum() / total
                     for year, rows in annual.groupby("year")
                     if (total := rows.units.sum()) > 0}
            print(line("  LP unit share %", share, fmt="%7.1f"))
            breaches = {year: round(share[year] - want, 2) for year, want in LP_BAND.items()
                        if year in share and abs(share[year] - want) > LP_TOLERANCE}
            print("     LP band: " + ("PASS" if not breaches else f"**FAIL** {breaches}"))

        rollup = read(name, "decade-annual-rollup")
        if rollup is not None and "singleUnits" in rollup:
            units = (rollup.singleUnits + rollup.albumUnits).sum()
            print(f"     total market units {units:,.0f}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("run")
    parser.add_argument("--baseline", default=None)
    arguments = parser.parse_args()
    report(arguments.run, arguments.baseline)
    return 0


if __name__ == "__main__":
    sys.exit(main())
