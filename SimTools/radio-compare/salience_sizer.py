"""Size ArtistEvolutionPressureService's salience constants against a measured trigger mix.

WHY THIS IS AN OFFLINE TOOL AND NOT A RUN
-----------------------------------------
The salience constants are causally inert on the simulation. `Evaluate` computes the six
pressures, then:

  * `restlessness` is `max(raw pressures) - resistance` -- RAW, never normalised, and it is
    the only pressure output the supply weight reads (`GenreSupplyService.GetRestlessness`);
  * `rootsMode` compares raw pressures to each other;
  * salience appears in `Dominant()` alone, whose outputs are `dominantTrigger` (a label),
    `dominantSalience` (compared against `ClimateScore` to produce one other label) and, via
    `DerivePhase`, `ArtistArcPhase` -- which is read only by the discography UI and the era
    summary composer.

So changing a salience constant cannot move a genre, a unit, or a conversion count. It can
only relabel conversions that already happened. That makes the trigger mix a PURE FUNCTION of
columns already written to `<run>-artist-evolution.csv`, and this script evaluates it exactly
rather than approximately -- `--verify` reproduces the recorded `trigger` column on every row
the salience vector governs, which is how you know the model here matches the C#.

Two classes of row are invariant under salience and are passed through unchanged:
  * `BackToRoots`, decided before `Dominant()` is consulted (candidate == formation genre);
  * the Phase-1 fallback (`restlessness <= 0`), whose inputs -- consecutiveFlops /
    consecutiveHits -- are not in the ledger.

USAGE
    python salience_sizer.py evo12 evo12b --verify
    python salience_sizer.py evo12 evo12b --mix
    python salience_sizer.py evo12 evo12b --fit
    python salience_sizer.py evo12 evo12b --mix --salience commercial=.45,peer=.22
"""

from __future__ import annotations

import argparse
import itertools
import sys
from pathlib import Path

import numpy as np
import pandas as pd

SIMLOGS = Path(__file__).resolve().parents[2] / "SimLogs"

# The vector evo12 / evo12b were produced at. `--verify` must be run against the vector the
# RUN used, not against whatever the C# holds now, or it is testing the wrong thing.
EVO12 = {
    "commercial": 0.45, "artistic": 0.52, "critical": 0.30,
    "peer": 0.30, "label": 0.36, "internal": 0.58, "climate": 1.00,
}

# Mirrors ArtistEvolutionPressureService + ArtistEvolutionService.ClimateScore as they stand.
CURRENT = {
    "commercial": 0.45,
    "artistic": 0.48,
    "critical": 0.23,
    "peer": 0.21,
    "label": 0.26,
    "internal": 0.49,
    # Not a salience: a multiplier on ArtistEvolutionService.ClimateScore, which is judged on
    # the same normalised scale and is therefore part of the same sizing problem. Lowering the
    # six scales raises every internal motive's score at once, so the climate argument loses
    # ground unless it moves with them. Belongs here because it is inert in exactly the same
    # way -- it decides a label and nothing else. 2.00 == ClimateScore's authored 1.15 / 0.35
    # doubled to 2.30 / 0.70, which is where the fit put it.
    "climate": 2.00,
}

# (ledger column, salience key, trigger). Order is immaterial -- Consider() takes a strict
# max, so ties go to the earlier entry in the C# and to the earlier entry here identically.
CHANNELS = [
    ("commercialPressure", "commercial", "CommercialFailure"),
    ("artisticPressure", "artistic", "PersonalAmbition"),
    ("criticalPressure", "critical", "CriticalBreakthrough"),
    ("labelPressure", "label", "LabelPressure"),
    ("internalPressure", "internal", "InternalTension"),
]
PRESSURES = [channel[0] for channel in CHANNELS] + ["peerPressure"]

# ArtistEvolutionService.ClimateScore.
CLIMATE_EMERGING = 1.15
CLIMATE_ESTABLISHED = 0.35

# The mix this branch is aiming at, as shares of ratified conversions. Commercial failure
# stays the single largest motive because most acts really are chasing a hit; what the
# directive asks for is that it stop being the ONLY thing anyone is ever doing.
DEFAULT_TARGET = {
    "CommercialFailure": 30.0,
    "PersonalAmbition": 24.0,
    "InternalTension": 14.0,
    "PeerInfluence": 11.0,
    "CriticalBreakthrough": 7.0,
    "LabelPressure": 6.0,
    "GenreClimateShift": 4.0,
    "CohesiveAlbumMovement": 1.5,
}


def load_lifecycle() -> dict:
    """emergence/death years, for reproducing GenreCatalog.GetLifecycle offline."""
    for path in sorted(SIMLOGS.glob("*-genre-catalog.csv")):
        catalog = pd.read_csv(path)
        return {row.genre: (row.emergenceYear, row.deathYear) for row in catalog.itertuples()}
    raise SystemExit("no <run>-genre-catalog.csv in SimLogs; cannot reproduce the climate score")


def climate_score(lifecycle: dict, genre: str, year: int) -> float:
    if genre not in lifecycle:
        return 0.0
    emergence, death = lifecycle[genre]
    if year < emergence:
        return 0.0
    if pd.notna(death) and year > death:
        return 0.0
    if year < emergence + 1:
        return CLIMATE_EMERGING
    if pd.notna(death) and year > death - 1:
        return 0.0
    return CLIMATE_ESTABLISHED


def load_run(run: str, lifecycle: dict) -> pd.DataFrame:
    path = SIMLOGS / f"{run}-artist-evolution.csv"
    if not path.exists():
        raise SystemExit(f"missing {path}")
    frame = pd.read_csv(path)
    frame["restlessness"] = np.clip(frame[PRESSURES].max(axis=1) - frame.resistance, 0.0, 1.0)
    frame["climate"] = [climate_score(lifecycle, genre, year)
                        for genre, year in zip(frame.toGenre, frame.year)]
    frame["run"] = run
    return frame


def derive(frame: pd.DataFrame, salience: dict) -> pd.DataFrame:
    """ArtistEvolutionPressureService.Dominant + ArtistEvolutionService.DeriveTrigger."""
    count = len(frame)
    best = np.zeros(count)
    trigger = np.full(count, "None", dtype=object)
    for column, key, name in CHANNELS:
        value = frame[column].to_numpy()
        score = np.where(value > 0.0, value / salience[key], 0.0)
        wins = score > best
        best = np.where(wins, score, best)
        trigger = np.where(wins, name, trigger)
    # The peer channel resolves to a different motive depending on the KIND of record that
    # reached the act, which the ledger records as influenceType.
    peer_name = np.where(frame.influenceType.to_numpy() == "CohesiveAlbum",
                         "CohesiveAlbumMovement", "PeerInfluence")
    value = frame.peerPressure.to_numpy()
    score = np.where(value > 0.0, value / salience["peer"], 0.0)
    wins = score > best
    best = np.where(wins, score, best)
    trigger = np.where(wins, peer_name, trigger)

    governed = (trigger != "None") & (frame.restlessness.to_numpy() > 0.0) \
        & (frame.trigger.to_numpy() != "BackToRoots")
    predicted = np.where(frame.climate.to_numpy() * salience["climate"] > best,
                         "GenreClimateShift", trigger)
    return pd.DataFrame({
        "governed": governed,
        "salience": best,
        # Rows the salience vector does not govern keep whatever the run recorded: BackToRoots
        # is settled before Dominant() runs, and the Phase-1 fallback reads flop/hit counts
        # that never reach the ledger.
        "trigger": np.where(governed, predicted, frame.trigger.to_numpy()),
    }, index=frame.index)


def verify(frames: dict, salience: dict) -> int:
    failures = 0
    for run, frame in frames.items():
        derived = derive(frame, salience)
        governed = frame[derived.governed]
        disagrees = derived.trigger[derived.governed] != governed.trigger
        # The ledger rounds every pressure to four decimals, so two channels can be a genuine
        # tie here that were microscopically apart in the C#. Those rows are a precision limit
        # of the CSV, not a modelling error: the winner's own score still reproduces exactly, so
        # count a disagreement as real only when this model's winner beats the recorded winner's
        # score by more than rounding can explain.
        rounding_tie = disagrees & (
            (derived.salience[derived.governed] - governed.dominantSalience).abs() < 1e-3)
        real = int((disagrees & ~rounding_tie).sum())
        agreement = 1.0 - real / max(1, len(governed))
        salience_error = (derived.salience[derived.governed] - governed.dominantSalience).abs().max()
        ok = real == 0 and salience_error < 5e-4
        failures += 0 if ok else 1
        print(f"{run:10s} governed {derived.governed.sum():6d}/{len(frame):6d}  "
              f"label agreement {agreement:.4f}  max salience error {salience_error:.5f}  "
              f"rounding ties {int(rounding_tie.sum())}  {'OK' if ok else 'MISMATCH'}")
        if not ok:
            print(governed[(disagrees & ~rounding_tie).to_numpy()].head(10).to_string())
    return failures


def mix(frames: dict, salience: dict) -> pd.DataFrame:
    columns = {}
    for run, frame in frames.items():
        derived = derive(frame, salience)
        ratified = derived.trigger[frame.ratified.to_numpy()]
        columns[run] = (pd.Series(ratified).value_counts(normalize=True) * 100).round(1)
    table = pd.DataFrame(columns).fillna(0.0)
    return table.sort_values(table.columns[0], ascending=False)


def fit(frames: dict, target: dict, anchor: str = "commercial") -> dict:
    """Coordinate descent on the five free scales, with one held fixed.

    Only RATIOS between scales matter -- multiplying every scale by a constant leaves every
    comparison unchanged -- so one is pinned or the search wanders a flat direction forever.
    """
    salience = dict(CURRENT)
    keys = [key for key in salience if key != anchor]

    def loss(candidate: dict) -> float:
        table = mix(frames, candidate)
        total = 0.0
        for name, want in target.items():
            got = float(table.loc[name].mean()) if name in table.index else 0.0
            total += (got - want) ** 2
        return total

    best = loss(salience)
    for sweep, step in itertools.product(range(6), [1.6, 1.25, 1.10, 1.04]):
        improved = False
        for key in keys:
            for factor in (step, 1.0 / step):
                candidate = dict(salience)
                candidate[key] = round(salience[key] * factor, 4)
                if candidate[key] <= 0.005:
                    continue
                score = loss(candidate)
                if score < best - 1e-9:
                    best, salience, improved = score, candidate, True
        if not improved and step == 1.04:
            break
    print(f"fit loss {best:.2f} (sum of squared share-point errors, averaged over seeds)")
    return salience


def parse_salience(text: str) -> dict:
    salience = dict(CURRENT)
    for pair in text.split(","):
        key, _, value = pair.partition("=")
        key = key.strip()
        if key not in salience:
            raise SystemExit(f"unknown pressure '{key}'; expected one of {sorted(salience)}")
        salience[key] = float(value)
    return salience


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("runs", nargs="+", help="run names, e.g. evo12 evo12b")
    parser.add_argument("--verify", action="store_true",
                        help="reproduce the recorded trigger column at the vector the run used "
                             "(--salience, else the EVO12 vector)")
    parser.add_argument("--mix", action="store_true", help="trigger mix over ratified conversions")
    parser.add_argument("--fit", action="store_true", help="solve for a salience vector")
    parser.add_argument("--salience", help="override, e.g. peer=.22,label=.20")
    parser.add_argument("--anchor", default="commercial", help="scale held fixed while fitting")
    arguments = parser.parse_args()

    lifecycle = load_lifecycle()
    frames = {run: load_run(run, lifecycle) for run in arguments.runs}

    if arguments.verify or not (arguments.mix or arguments.fit):
        if verify(frames, parse_salience(arguments.salience) if arguments.salience else EVO12):
            return 1

    salience = parse_salience(arguments.salience) if arguments.salience else dict(CURRENT)

    if arguments.fit:
        salience = fit(frames, DEFAULT_TARGET, arguments.anchor)
        print("fitted salience: " + ", ".join(f"{k}={v:.4f}" for k, v in salience.items()))

    if arguments.mix or arguments.fit:
        print("\nsalience: " + ", ".join(f"{k}={v:.4f}" for k, v in salience.items()))
        table = mix(frames, salience)
        table["target"] = [DEFAULT_TARGET.get(name, np.nan) for name in table.index]
        print(table.to_string())
    return 0


if __name__ == "__main__":
    sys.exit(main())
