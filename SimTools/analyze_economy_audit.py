"""Read-only measurements behind SimTools/EconomyReleaseJudgementAudit-2026-10-20.md.

Usage (full-telemetry runs, not --calibration):
    python analyze_economy_audit.py <SimLogs/run-prefix> [<album-chart.csv>]

1. Format memory: provisional (AgeN) outcome estimates against the release's Final outcome, and the
   recency x maturity weighted residual the live AlbumComponent estimator reads at each year boundary
   (pooled across labels) against what the same releases' final residuals say.
2. Priors against realized net by career band and quality quartile; format share by career band.
3. Share of memory-eligible releases with negative realized net (before marketing), by format/period/tier.
4. Distribution advances signed (never collected back by the client; see audit B3).
5. Optional: album chart peaks scored against the singles-ladder thresholds the career code applies.
"""
import sys
from collections import defaultdict
from statistics import median

import pandas as pd

run = sys.argv[1]
album_chart = sys.argv[2] if len(sys.argv) > 2 else None
pd.set_option("display.width", 200)


def section(title):
    print(f"\n=== {title} ===")


# 1. Format memory -----------------------------------------------------------------------------------
section("1a. Provisional estimate vs final realized net (median, per lane and age)")
rev = pd.read_csv(f"{run}-format-memory-revisions.csv",
                  usecols=["week", "releaseId", "estimatorLane", "releaseAge", "revisionKind", "normalizedResidual",
                           "maturityWeight", "realizedNetToDate", "estimatedOutcomeNet", "opportunityScale"])
final = rev[rev.revisionKind == "Final"].set_index(["releaseId", "estimatorLane"])["realizedNetToDate"]
prov = rev[rev.revisionKind != "Final"].join(final.rename("finalNet"), on=["releaseId", "estimatorLane"], how="inner")
prov["overshoot"] = (prov.estimatedOutcomeNet - prov.finalNet) / prov.opportunityScale.clip(lower=1.0)
g = prov.groupby(["estimatorLane", "releaseAge"]).agg(n=("overshoot", "size"), medOvershoot=("overshoot", "median"),
                                                       shareOver=("overshoot", lambda s: (s > 0).mean()),
                                                       medEstimate=("estimatedOutcomeNet", "median"), medFinal=("finalNet", "median"))
print(g[g.n >= 200].round(3).to_string())

section("1b. AlbumComponent decision-time residual: live blend vs final-only (pooled)")
lane = rev[rev.estimatorLane == "AlbumComponent"].copy()
release_week = lane.groupby("releaseId")["week"].min() - lane.groupby("releaseId")["releaseAge"].min()
lane["releaseWeek"] = lane["releaseId"].map(release_week)
finals = lane[lane.revisionKind == "Final"].set_index("releaseId")["normalizedResidual"]
for W in range(104, int(lane.week.max()) + 1, 52):
    seen = lane[lane.week <= W].sort_values("week").groupby("releaseId").tail(1)
    seen = seen[(W - seen.releaseWeek) <= 104]
    rec = 0.5 ** ((W - seen.releaseWeek) / 52.0)
    w = rec * seen.maturityWeight.clip(0.05, 1.0)
    live = (seen.normalizedResidual * w).sum() / w.sum()
    provisional = w[seen.revisionKind != "Final"].sum() / w.sum()
    fin = seen.releaseId.map(finals)
    ok = fin.notna()
    final_blend = (fin[ok] * rec[ok]).sum() / rec[ok].sum() if ok.any() else float("nan")
    print(f"week {W:4d}: live {live:+.3f}  final-only {final_blend:+.3f}  provisional weight {provisional:.0%}  n={len(seen)}")

# 2. Priors vs realized ------------------------------------------------------------------------------
out = pd.read_csv(f"{run}-release-outcomes.csv")
out = out[out.memoryEligible == True]
dec = pd.read_csv(f"{run}-a3-economic-decisions.csv",
                  usecols=["recordId", "careerBand", "qualityQuartile", "statureMultiplier", "priorSingleNet",
                           "priorAlbumNet", "chosenFormat", "year"])
d = dec.merge(out[["recordId", "realizedNet", "format"]], on="recordId", how="inner")
section("2a. Orphan singles: prior vs realized net by career band x quality quartile (medians)")
s = d[(d.chosenFormat == "Single") & (d.format == "Single")]
t = s.groupby(["careerBand", "qualityQuartile"])[["priorSingleNet", "realizedNet"]].median().round(0)
t["n"] = s.groupby(["careerBand", "qualityQuartile"]).size()
print(t.to_string())
section("2b. Albums: prior vs realized net by career band (medians)")
a = d[(d.chosenFormat == "Album") & (d.format == "Album")]
t = a.groupby("careerBand")[["statureMultiplier", "priorAlbumNet", "realizedNet"]].median().round(1)
t["n"] = a.groupby("careerBand").size()
t["realized/prior"] = (t.realizedNet / t.priorAlbumNet).round(2)
print(t.to_string())
section("2c. Album share of format decisions by career band and period")
dec["period"] = pd.cut(dec.year, [1959, 1962, 1965, 1970], labels=["1960-62", "1963-65", "1966-69"])
print(dec.assign(album=dec.chosenFormat == "Album").groupby(["careerBand", "period"], observed=True)["album"]
      .agg(["count", "mean"]).round(2).to_string())

# 3. Money-losing releases ---------------------------------------------------------------------------
section("3. Share of releases with realizedNet < 0 (production and pressing in, marketing out)")
strat = pd.read_csv(f"{run}-release-strategy.csv", usecols=["recordId", "tier"])
m = out.merge(strat, on="recordId", how="left")
m["period"] = pd.cut(m.year, [1959, 1962, 1965, 1970], labels=["1960-62", "1963-65", "1966-69"])
m["loses"] = m.realizedNet < 0
print(m.groupby(["format", "period"], observed=True)["loses"].agg(["count", "mean"]).round(3).to_string())
print(m.groupby(["format", "tier"], observed=True)["loses"].agg(["count", "mean"]).round(3).to_string())

# 4. Distribution advances ---------------------------------------------------------------------------
section("4. Distribution advances signed")
deals = pd.read_csv(f"{run}-deal-ledger.csv")
signed = deals[deals.resolution == "Signed"]
print(signed.groupby("origin")["advance"].agg(["count", "sum", "median", "max"]).round(0).to_string())
print(signed.groupby("year")["advance"].agg(["count", "sum", "median"]).round(0).to_string())

# 5. Album chart peaks vs singles-ladder thresholds -----------------------------------------------------
if album_chart:
    section("5. Album chart peaks scored by the singles ladder (<=40 hit, 41-60 neutral, >60 flop)")
    ac = pd.read_csv(album_chart, usecols=["year", "chartSize", "position", "recordId"])
    peaks = ac.groupby("recordId").agg(peak=("position", "min"), year=("year", "min"))
    peaks["band"] = pd.cut(peaks.peak, [0, 40, 60, 100, 200],
                           labels=["<=40 hit", "41-60 neutral", "61-100 flop -0.10", "101-200 flop -0.15"])
    print(peaks.groupby(["year", "band"], observed=True).size().unstack(fill_value=0).to_string())
    print((peaks.peak > 60).groupby(peaks.year).mean().round(2).rename("flop share").to_string())
