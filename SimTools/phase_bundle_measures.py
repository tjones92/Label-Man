"""Post-Phase-4 bundle measures (SimTools/BandMemberSimulationDirective.md §14): treatment window vs control window.

Usage:  py SimTools/phase_bundle_measures.py <control-run> <treatment-run>

Beside SimTools/band_life_ab.py (calibration), this reads the phase gates:
  4c team writing -- mean hook / composition quality of released material, artist originals and professional songs
  4e wealth       -- (treatment's member log) the wealth gap writers vs non-writers in charting acts
  4f fatigue      -- exhaustion exits per year, and who goes studio-only (charting? rich?)
  5  growth       -- mean live technical skill of members in acts, per year (needs --log-band-life-members on both)
  all             -- departures by cause and kind, spin-outs, substance onsets, deaths
Diagnostic only.
"""
import os
import sys

import numpy as np
import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")


def read(run, name, **kw):
    path = os.path.join(LOGS, f"{run}-{name}.csv")
    return pd.read_csv(path, low_memory=False, **kw) if os.path.exists(path) else None


def main():
    ctl, trt = sys.argv[1], sys.argv[2]
    print(f"=== {trt} vs {ctl} ===")

    # 4c: material quality
    for run in (ctl, trt):
        sm = read(run, "song-material")
        if sm is None:
            continue
        own = sm[sm.songSource == "ArtistWritten"]
        pro = sm[sm.songSource == "ExternalProfessional"]
        print(f"[4c] {run:26s} hookStrength all {sm.hookStrengthAtRelease.mean():.4f} | originals n={len(own)} hook "
              f"{own.compositionHook.mean():.4f} comp {own.compositionQuality.mean():.4f} | pro n={len(pro)} hook "
              f"{pro.compositionHook.mean():.4f} comp {pro.compositionQuality.mean():.4f}")

    # lineup annual: causes, kinds, exhaustion
    cols = ["exhaustion", "strainDepartures", "spinOuts", "kindStudioOnly", "kindSoloCareer", "substanceOnsets",
            "deathSubstance", "dissolutions", "replacements"]
    causes = ["causeCreditAndMoney", "causeSpotlight", "causeDirection", "causeReliability", "causeRomance", "causeBurnout", "causeOutsider"]
    for run in (ctl, trt):
        la = read(run, "lineup-annual")
        if la is None:
            continue
        print(f"[annual] {run}")
        print(la[["year"] + cols].to_string(index=False))
        c = la[causes].sum()
        print("   cause mix:", (c / max(1, c.sum())).round(3).to_dict())

    # studio-only exits: charting? (events)
    for run in (ctl, trt):
        ev = read(run, "lineup-events")
        if ev is None:
            continue
        so = ev[ev.event == "studio-only"]
        print(f"[4f] {run}: studio-only {len(so)}, from charted acts {(so.everCharted.astype(str).str.lower() == 'true').mean():.1%};"
              f" solo spin-outs {(ev.event == 'solo-spinout').sum()}")

    # growth and wealth from member logs
    for run in (ctl, trt):
        mem = read(run, "lineup-members")
        if mem is None:
            continue
        g = mem.groupby("year").agg(tech=("technicalNow", "mean"), creativity=("creativity", "mean"), ego=("ego", "mean"),
                                    temperament=("temperament", "mean"), wealth=("wealth", "mean"), fatigue=("fatigue", "mean"))
        print(f"[5] {run} member means by year:\n{g.round(4).to_string()}")
    mem = read(trt, "lineup-members")
    if mem is not None:
        last = mem[mem.year == mem.year.max()]
        hit = last[(last.constitution != "Solo") & (last.everCharted == 1)]
        w, nw = hit[hit.writer == 1].wealth, hit[hit.writer == 0].wealth
        print(f"[4e] {trt} charting groups: writers median ${w.median():,.0f} q90 ${w.quantile(.9):,.0f} | non-writers median"
              f" ${nw.median():,.0f} q90 ${nw.quantile(.9):,.0f}")


if __name__ == "__main__":
    main()
