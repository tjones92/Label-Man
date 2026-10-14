"""Offline replay of band-life strain from an observe run's pair log (SimTools/BandMemberSimulationDirective.md §4.4, §4.6).

Usage:  py SimTools/fit_band_life_strain.py <run-name> [--scale=1.0] [--retention=0.6] [--w-credit=..] ...

Reads SimLogs/<run>-lineup-pairs.csv (written with --log-band-life-pairs) and replays every edge's strain level
year by year under candidate weights, then runs the stage ladder (Brewing -> Ultimatum -> Departure) to count
would-be strain departures per group-act-year. The replay is approximate where the log is: AI concessions and the
breaker are ignored, and a departure resets the act's edges. With the live weights it should land close to the
run's own strainDepartures; the fit is the weight set that puts charting groups near the target hazard while no
cause exceeds 45% of departures. Diagnostic only.

LIVE is the weight set the 2026-10-07 observe logs were written under (the pre-fit constants). The fitted constants now
in BandLifeService correspond to: --scale=1.45 --w-credit=2.0 --w-spot=2.5 --w-dir=0.25 --w-rel=0.30 --w-sub=0.95
--w-burn=0.35 --w-out=0.1 --friction=3.6 --solvent=0.77. A pair log written under the fitted constants already carries
StrainScale's friction and SolventScale in its projectFriction and solvent columns, so replay it with --friction=1
--solvent=1 and the remaining weights above.

--sub-mult scales the logged substanceRaw, to replay an older pair log at a newer substance stock. The bms2-obs logs
predate the habit-draw resize (b9c63be); --sub-mult=1.8 puts them at the 1967-68 stock of the bms5 runs (§15).
"""
import os
import sys
from collections import Counter, defaultdict

import numpy as np
import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")
CAUSES = ["CreditAndMoney", "Spotlight", "Direction", "Reliability", "Burnout", "Outsider"]

LIVE = dict(scale=1.0, retention=0.60, w_credit=0.60, w_spot=0.40, w_dir=0.25, w_rel=0.30, w_sub=0.20, w_burn=0.12,
            w_out=0.05, friction=1.0, r_dir=0.10, r_credit=0.05, brewing=0.45, ultimatum=0.65, departure=0.85,
            morale_shift=0.5, solvent=1.0, sub_mult=1.0)


def load(run):
    path = os.path.join(LOGS, f"{run}-lineup-pairs.csv")
    df = pd.read_csv(path)
    return df[df["row"] == "pair"].copy(), df[df["row"] == "act"].copy()


def replay(pairs, acts, p):
    pairs = pairs.sort_values(["artistId", "year"])
    terms = {
        "CreditAndMoney": p["w_credit"] * pairs["creditRaw"] + p["r_credit"] * pairs["rivalry"],
        "Spotlight": p["w_spot"] * pairs["spotlightRaw"],
        "Direction": p["w_dir"] * pairs["directionRaw"] + p["friction"] * pairs["projectFriction"] + p["r_dir"] * pairs["rivalry"],
        "Reliability": p["w_rel"] * pairs["reliabilityRaw"] + p["w_sub"] * p["sub_mult"] * pairs["substanceRaw"],
        "Outsider": p["w_out"] * pairs["outsiderRaw"],
    }
    mult = p["scale"] * pairs["scale"] * pairs["workFactor"]
    total = sum(terms.values()) * mult
    excess = np.maximum(0.0, total - p["solvent"] * pairs["solvent"]).to_numpy()
    tmat = np.vstack([terms[c].to_numpy() for c in terms]).T
    names = list(terms)

    morale = {(r.artistId, r.year): r.morale for r in acts.itertuples()}
    burn = {(r.artistId, r.year): (p["w_burn"] * r.burnoutRaw, r.personA, r.personB) for r in acts.itertuples()}
    ever40 = set()
    top40 = pairs.groupby(["artistId", "year"])["top40Now"].max()

    strain = {}         # (artist, a, b) -> level
    cause_w = {}        # (artist, a, b) -> np.array of cause weights
    brew = {}           # (artist, a, b) -> consecutive years at/above brewing
    departed_year = {}  # artist -> last departure year (cooldown)
    rows = []
    keys = list(zip(pairs["artistId"], pairs["year"], pairs["personA"], pairs["personB"]))
    charted = pairs["everCharted"].to_numpy()
    by_act_year = defaultdict(list)
    for i, (a, y, x, z) in enumerate(keys):
        by_act_year[(a, y)].append(i)
    for (a, y) in sorted(by_act_year, key=lambda k: (k[1], k[0])):
        idx = by_act_year[(a, y)]
        if top40.get((a, y), 0) > 0:
            ever40.add(a)
        for i in idx:
            k = (a, keys[i][2], keys[i][3])
            s = strain.get(k, 0.0) * p["retention"]
            cw = cause_w.get(k, np.zeros(len(names) + 1)) * p["retention"]
            if excess[i] > 0:
                s = min(1.0, s + excess[i])
                tot = tmat[i].sum()
                if tot > 0:
                    cw[:len(names)] += excess[i] * tmat[i] / tot
            strain[k], cause_w[k] = s, cw
        b, ba, bb = burn.get((a, y), (0.0, None, None))
        if b > 0.005 and idx:
            ks = [(a, keys[i][2], keys[i][3]) for i in idx]
            k = max(ks, key=lambda q: strain[q]) if any(strain[q] > 0 for q in ks) else (a, ba, bb)
            strain[k] = min(1.0, strain.get(k, 0.0) + b)
            cw = cause_w.get(k, np.zeros(len(names) + 1))
            cw[len(names)] += b
            cause_w[k] = cw
        m = morale.get((a, y), 0.0)
        leave = None
        for i in idx:
            k = (a, keys[i][2], keys[i][3])
            s = strain[k] - m * p["morale_shift"]
            if s >= p["brewing"]:
                brew[k] = brew.get(k, 0) + 1
            else:
                brew[k] = 0
            # Brewing, then Ultimatum the next year at >= brewing (or at once at >= ultimatum), then departure the year
            # after the ultimatum if still >= brewing; or at once at >= departure.
            ult_year = 1 if s >= p["ultimatum"] else 2
            if s >= p["departure"] or brew[k] > ult_year:
                if leave is None or strain[k] > strain[leave]:
                    leave = k
        if leave is not None and departed_year.get(a, -9) < y:
            cw = cause_w[leave]
            cause = (names + ["Burnout"])[int(np.argmax(cw))]
            rows.append((y, a, bool(charted[idx[0]]), a in ever40, cause))
            departed_year[a] = y
            for i in idx:
                k = (a, keys[i][2], keys[i][3])
                strain[k] = 0.0
                cause_w[k] = np.zeros(len(names) + 1)
                brew[k] = 0
    deps = pd.DataFrame(rows, columns=["year", "artistId", "everCharted", "everTop40", "cause"]).astype({"everCharted": bool, "everTop40": bool})
    actyears = pairs.groupby(["artistId", "year"])["everCharted"].max().reset_index()
    actyears["everTop40"] = actyears["artistId"].isin(ever40)
    return deps, actyears


def report(run, p):
    pairs, acts = load(run)
    deps, ay = replay(pairs, acts, p)
    out = []
    out.append(f"=== {run}  " + " ".join(f"{k}={v}" for k, v in p.items() if LIVE.get(k) != v))
    out.append("year  groupActs  charted  top40 | deps  depCharted  depTop40 | hazard charted  top40  never")
    for y in sorted(ay["year"].unique()):
        a = ay[ay["year"] == y]
        d = deps[deps["year"] == y]
        nc, n40 = int(a["everCharted"].sum()), int(a["everTop40"].sum())
        nn = len(a) - nc
        dc, d40 = int(d["everCharted"].sum()), int(d["everTop40"].sum())
        dn = len(d) - dc
        out.append(f"{y}  {len(a):9d}  {nc:7d}  {n40:5d} | {len(d):4d}  {dc:10d}  {d40:8d} | "
                   f"{dc / max(1, nc):14.3f}  {d40 / max(1, n40):5.3f}  {dn / max(1, nn):5.3f}")
    nc, n40 = int(ay["everCharted"].sum()), int(ay["everTop40"].sum())
    out.append(f"decade hazard: charted {deps['everCharted'].sum() / max(1, nc):.3f}  top40 {deps['everTop40'].sum() / max(1, n40):.3f}  "
               f"never {(~deps['everCharted']).sum() / max(1, len(ay) - nc):.4f}  total deps {len(deps)}")
    mix = Counter(deps["cause"])
    tot = max(1, sum(mix.values()))
    out.append("cause mix: " + ", ".join(f"{c} {mix.get(c, 0) / tot:.0%}" for c in CAUSES))
    mixc = Counter(deps[deps["everCharted"]]["cause"])
    totc = max(1, sum(mixc.values()))
    out.append("cause mix (charted): " + ", ".join(f"{c} {mixc.get(c, 0) / totc:.0%}" for c in CAUSES))
    print("\n".join(out))


if __name__ == "__main__":
    params = dict(LIVE)
    runs = []
    for arg in sys.argv[1:]:
        if arg.startswith("--"):
            k, v = arg[2:].split("=")
            params[k.replace("-", "_")] = float(v)
        else:
            runs.append(arg)
    for r in runs:
        report(r, params)
