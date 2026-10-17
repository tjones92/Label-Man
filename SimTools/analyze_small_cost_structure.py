"""Small-cohort costs and paired home-territory closures, from existing telemetry.

Usage: python SimTools/analyze_small_cost_structure.py PREFIX [PREFIX ...]
Static cost replays stop at actual closure; they are diagnostics, not simulated survival.
"""
import json
import sys
from pathlib import Path

import numpy as np
import pandas as pd

CLOSED = ["Bankrupt", "Defunct"]


def analyze(prefix):
    fin = pd.read_csv(prefix + "-label-finance.csv")
    opening = fin[fin.week == 1].set_index("labelId")
    ids = opening.index[opening.labelTier == "Small"]
    fin = fin[fin.labelId.isin(ids)].sort_values(["labelId", "week"])
    end = fin.groupby("labelId").last()
    closed = end.index[end.status.isin(CLOSED)]
    out = {"prefix": prefix, "weeks": int(fin.week.max()), "cohort": len(ids),
           "closed_count": len(closed), "closed_pct": 100 * len(closed) / len(ids)}
    outcomes = pd.read_csv(prefix + "-release-outcomes.csv")
    outcomes = outcomes[outcomes.labelId.isin(ids)]
    # Read the settlement once, with only the columns needed for cost decomposition.
    columns = ["week", "recordId", "labelId", "format", "totalUnits", "gross", "manufacturingCost",
               "distributionSkim", "labelNet", "pressingCost"]
    settlement = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=columns)
    settlement = settlement[settlement.labelId.isin(ids)]
    singles = settlement[settlement.format == "Single"]
    for name, mask in [("closed", fin.labelId.isin(closed)), ("other", ~fin.labelId.isin(closed))]:
        f = fin[mask]
        s = settlement[settlement.labelId.isin(f.labelId.unique())]
        o = outcomes[outcomes.labelId.isin(f.labelId.unique()) & (outcomes.format == "Single")]
        totals = f.groupby("labelId")[["weeklyGross", "weeklyPressing", "weeklyNet"]].sum()
        final = f.groupby("labelId").last()
        # Overhead debits occur at the monthly review, identifiable by a change in the
        # insolvent-review streak / cash-flow snapshot; weeklyOverhead / 4.33 is only
        # a burden approximation, explicitly labelled below.
        out[name] = {"labels": len(final), "singles_retired": len(o),
            "median_single_units": float(o.pressingBookSold.median()),
            "median_single_net": float(o.realizedNet.median()),
            "pressing_over_gross": float(s.pressingCost.sum() / max(1, s.gross.sum())),
            "median_lifetime_gross": float(totals.weeklyGross.median()),
            "median_end_receivables": float(final.outstandingWholesaleReceivables.median()),
            "median_end_monthly_overhead": float(final.monthlyOverhead.median()),
            "approx_overhead_total": float(f.loc[~f.status.isin(CLOSED + ["Acquired"]), "monthlyOverhead"].sum() * 12 / 52),
            "closed_ids": list(final.index) if name == "closed" else []}
    # Preserve the old skim fraction after manufacturing. Physical 45 costs are
    # contrasted with the existing 30c bundle, keeping 4c per SOLD single for two
    # compositions. Setup/shipping are deliberately omitted: this is an upper bound
    # on relief. This books relief immediately and ignores collection/credit delays;
    # it cannot establish whether a particular cash insolvency would be averted.
    counterfactual = []
    for physical in [.12, .14, .18, .22]:
        delta = singles.copy()
        delta["saving"] = delta.pressingCost * (1 - physical / .30) - .04 * delta.totalUnits
        # The skim base grows when manufacturing gets cheaper.
        fraction = delta.distributionSkim / (delta.gross - .30 * delta.totalUnits).clip(lower=.001)
        delta["saving"] -= (.30 - physical - .04) * delta.totalUnits * fraction.clip(0, 1)
        byweek = delta.groupby(["labelId", "week"]).saving.sum()
        idx = pd.MultiIndex.from_frame(fin[["labelId", "week"]])
        saving = pd.Series(byweek.reindex(idx, fill_value=0).to_numpy(), index=fin.index)
        accumulated = saving.groupby(fin.labelId).cumsum()
        cash = fin.cashReserves + accumulated
        rescued = closed.intersection(fin.loc[(fin.status.isin(CLOSED)) & (cash >= 0), "labelId"].unique())
        counterfactual.append({"physical_cents": physical * 100,
            "composition_cents_on_sales": 4, "actual_closures_with_nonnegative_replayed_end_cash": len(rescued)})
    out["static_upper_bound_relief"] = counterfactual
    transitions_path = Path(prefix + "-label-tier-transitions.csv")
    if transitions_path.exists():
        t = pd.read_csv(transitions_path)
        t = t[t.labelId.isin(closed)]
        out["closed_previously_promoted"] = int(t.loc[t.direction == "promotion", "labelId"].nunique())
    return out


if __name__ == "__main__":
    rows = [analyze(p) for p in sys.argv[1:]]
    if len(rows) == 2:
        a, b = [set(r["closed"]["closed_ids"]) for r in rows]
        rows.append({"only_first_closed": sorted(a - b), "only_second_closed": sorted(b - a),
                     "both_closed": len(a & b)})
    print(json.dumps(rows, indent=2, allow_nan=True))
