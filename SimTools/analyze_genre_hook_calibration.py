"""Reproducible genre/head diagnosis, with seed-level uncertainty and startup decomposition.

Example: python SimTools/analyze_genre_hook_calibration.py --tags rj2asp10-ctl awrw-on awkf-on awpp-on
Outputs are scratch artifacts in SimLogs; no simulation settings are changed.
"""
import argparse
import re
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
BIG5 = {f"label_{i:04d}" for i in range(1, 6)}


def norm(value):
    value = re.sub(r"[^a-z0-9]", "", re.sub(r"\(.*?\)", "", value).lower())
    return {"rb": "rnb"}.get(value, value)


def slot_targets():
    # Use the live game's version, rather than an analysis script's duplicate table.
    text = (ROOT / "Data/GenreCatalog.cs").read_text(encoding="utf-8-sig")
    block = text.split("ChartSlotBenchmark = new() {", 1)[1].split("};", 1)[0]
    rows = re.findall(r"\[Genre\.(\w+)\]\s*=\s*new\[\]\s*\{([^}]+)\}", block)
    result = {}
    for genre, numbers in rows:
        values = [int(v.strip()) for v in numbers.split(",") if v.strip()]
        if len(values) != 10:
            raise ValueError(f"Expected ten benchmark years for {genre}")
        result.update({(norm(genre), 1960 + i): v for i, v in enumerate(values)})
    if not result:
        raise ValueError("Chart-slot benchmark was not found")
    return result


def measure(prefix, seed, tag, targets, slots):
    r = pd.read_csv(f"{prefix}-records.csv", usecols=[
        "week", "year", "recordId", "labelId", "genre", "isPlayerOwned",
        "currentPosition", "weeksSinceRelease"])
    r = r[r.currentPosition.between(1, 100) & ~r.isPlayerOwned.astype(str).str.lower().eq("true")].copy()
    if r.duplicated(["week", "recordId"]).any():
        raise ValueError(f"Duplicate chart records in {prefix}")
    q = pd.read_csv(f"{prefix}-single-launch-quality.csv")
    shape = pd.read_csv(f"{prefix}-genre-decade-shape.csv")
    ye = pd.read_csv(f"{prefix}-year-end-hot100.csv")
    economy = pd.read_csv(f"{prefix}-decade-annual-rollup.csv").set_index("year")
    formats_path = Path(f"{prefix}-genre-format-units.csv")
    formats = pd.read_csv(formats_path) if formats_path.exists() else None
    if formats is not None:
        reconciled = formats.groupby(["year", "genre"]).units.sum().rename("formatUnits").reset_index().merge(
            shape[["year", "genre", "marketUnits"]], on=["year", "genre"], how="outer").fillna(0)
        if not (reconciled.formatUnits == reconciled.marketUnits).all():
            raise ValueError(f"Format units do not reconcile to genre totals: {prefix}")
    first = r.sort_values("week").drop_duplicates("recordId")
    # Use the complete launch ledger: a fresh week-one release also has age one,
    # so comparing record age with audit week alone misclassifies those releases.
    if q.singleId.duplicated().any():
        raise ValueError(f"Duplicate launches in {prefix}")
    inherited = set(first.recordId) - set(q.singleId)
    r["inherited"] = r.recordId.isin(inherited)
    r["big5"] = r.labelId.isin(BIG5)
    output, detail = [], []

    def put(year, metric, value):
        output.append(dict(tag=tag, seed=seed, year=int(year), metric=metric, value=float(value)))

    for year, annual in r.groupby("year"):
        weeks = annual.week.nunique()
        if weeks != 52 or len(annual) != 5200:
            raise ValueError(f"Incomplete chart year {prefix} {year}: {weeks} weeks, {len(annual)} rows")
        head = annual[annual.currentPosition <= 10]
        labels = head.groupby("labelId").size()
        put(year, "CR8", 100 * labels.nlargest(8).sum() / len(head))
        put(year, "CR4", 100 * labels.nlargest(4).sum() / len(head))
        put(year, "Big5", 100 * head.big5.mean())
        put(year, "Top10Titles", head.recordId.nunique())
        put(year, "Big5Top10Titles", head.loc[head.big5, "recordId"].nunique())
        put(year, "InheritedTop10", 100 * head.inherited.mean())
        put(year, "Big5InheritedContribution", 100 * (head.big5 & head.inherited).mean())
        put(year, "Big5NewContribution", 100 * (head.big5 & ~head.inherited).mean())
        put(year, "Debuts", (first.year == year).sum())
        put(year, "MarketUnits", shape.loc[shape.year == year, "marketUnits"].sum())
        launches = q[q.year == year]
        put(year, "SingleLaunches", len(launches))
        put(year, "PromoLaunchFraction", 100 * launches.strategy.eq("AlbumWithPromo").mean())
        for metric, column in (("SingleUnits", "singleUnits"), ("AlbumUnits", "albumUnits"),
                               ("SingleGross", "singleGross"), ("AlbumGross", "albumGross"),
                               ("AlbumToSingleGross", "albumToSingleGross")):
            put(year, metric, economy.loc[year, column])
        for quarter in range(4):
            h = head[head.week.between((year - 1960) * 52 + quarter * 13 + 1,
                                     (year - 1960) * 52 + (quarter + 1) * 13)]
            put(year, f"Big5Q{quarter + 1}", 100 * h.big5.mean())
            put(year, f"CR8Q{quarter + 1}", 100 * h.groupby("labelId").size().nlargest(8).sum() / len(h))
        actual_share = {norm(row.genre): 100 * row.marketUnitsShare
                        for row in shape[shape.year == year].itertuples()}
        actual_slots = {norm(row.genre): row.yearEndSlots for row in ye[ye.year == year].itertuples()}
        genres = set(actual_share) | {g for g, y in targets if y == year}
        put(year, "ShareError", sum(abs(actual_share.get(g, 0) - targets.get((g, year), 0)) for g in genres))
        genres = set(actual_slots) | {g for g, y in slots if y == year}
        put(year, "SlotError", sum(abs(actual_slots.get(g, 0) - slots.get((g, year), 0)) for g in genres))
        slot_total = sum(v for (g, y), v in slots.items() if y == year)
        put(year, "SlotTargetTotal", slot_total)
        put(year, "SlotErrorLowerBound", abs(slot_total - sum(actual_slots.values())))
        # Sensitivity only: overlapping genre tags are not made mutually exclusive by normalization.
        put(year, "SlotErrorNormalized", sum(abs(actual_slots.get(g, 0) - 100 * slots.get((g, year), 0)
                                               / slot_total) for g in genres))
        for genre, group in annual.groupby("genre"):
            h = group[group.currentPosition <= 10]
            launches = q[(q.year == year) & (q.genre == genre)]
            put(year, f"Share:{genre}", actual_share.get(norm(genre), 0))
            put(year, f"Slots:{genre}", actual_slots.get(norm(genre), 0))
            put(year, f"Top10:{genre}", 100 * len(h) / len(head))
            put(year, f"Big5Top10:{genre}", 100 * h.big5.sum() / len(head))
            if formats is not None:
                f = formats[formats.year == year]
                genre_units = f.loc[f.genre == genre, "units"].sum()
                for fmt in ("Single", "Album"):
                    total = f.loc[f.format == fmt, "units"].sum()
                    units = f.loc[(f.genre == genre) & (f.format == fmt), "units"].sum()
                    put(year, f"{fmt}Share:{genre}", 100 * units / total if total else 0)
                    if fmt == "Single":
                        put(year, f"SingleFraction:{genre}", 100 * units / genre_units if genre_units else 0)
            for (tier, strategy), cohort in launches.groupby(["tier", "strategy"]):
                detail.append(dict(tag=tag, seed=seed, year=int(year), genre=genre, tier=tier,
                                   strategy=strategy, launches=len(cohort), hook=cohort.hook.mean(),
                                   production=cohort.production.mean()))
    return output, detail


def summarize(frame, keys):
    out = frame.groupby(keys).value.agg(["mean", "std", "count"]).reset_index()
    out["se"] = out["std"] / out["count"].pow(.5)
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--tags", nargs="+", required=True)
    parser.add_argument("--seeds", nargs="+", type=int, default=[1001, 2002, 3003, 4004, 5005])
    parser.add_argument("--weeks", type=int, default=260)
    parser.add_argument("--control-weeks", type=int, help="First tag's duration, if a short screen reuses a longer control")
    parser.add_argument("--output", default="SimLogs/genre-hook-investigation")
    args = parser.parse_args()
    if len(set(args.seeds)) != len(args.seeds):
        parser.error("Seeds must be distinct")
    table = pd.read_csv(ROOT / "SimTools/AdjustedHistoricalGenreShareTargets.csv")
    targets = {(norm(row["genre"]), int(y)): row[y] for _, row in table.iterrows()
               for y in table.columns if y.isdigit()}
    output, detail = [], []
    for tag in args.tags:
        for seed in args.seeds:
            weeks = args.control_weeks if tag == args.tags[0] and args.control_weeks is not None else args.weeks
            prefix = ROOT / f"SimLogs/{tag}-{seed}-{weeks}"
            rows, cohorts = measure(prefix, seed, tag, targets, slot_targets())
            output.extend(rows)
            detail.extend(cohorts)
    rows = pd.DataFrame(output)
    out = ROOT / args.output
    out.parent.mkdir(parents=True, exist_ok=True)
    rows.to_csv(f"{out}-seeds.csv", index=False)
    pd.DataFrame(detail).to_csv(f"{out}-cohorts.csv", index=False)
    summary = summarize(rows, ["tag", "year", "metric"])
    summary.to_csv(f"{out}-summary.csv", index=False)
    base = rows[rows.tag == args.tags[0]].drop(columns="tag").rename(columns={"value": "control"})
    paired = rows[rows.tag != args.tags[0]].merge(base, on=["seed", "year", "metric"], validate="many_to_one")
    paired["value"] -= paired.control
    deltas = summarize(paired, ["tag", "year", "metric"])
    deltas.to_csv(f"{out}-paired.csv", index=False)
    selected = ["CR8", "Big5", "InheritedTop10", "Big5InheritedContribution", "Big5NewContribution",
                "Share:RockAndRoll", "Share:TraditionalPop", "Share:Soul", "SlotError", "ShareError"]
    print(summary[summary.metric.isin(selected)].pivot(index=["tag", "metric"], columns="year", values="mean").round(2).to_string())
    print("\n1960 seed values and standard errors")
    print(rows[(rows.year == 1960) & rows.metric.isin(["CR8", "Big5"])].pivot(
        index=["tag", "metric"], columns="seed", values="value").round(2).to_string())
    print(summary[(summary.year == 1960) & summary.metric.isin(["CR8", "Big5"])].round(2).to_string(index=False))
    print(f"\nArtifacts: {out}-{{seeds,cohorts,summary,paired}}.csv")


if __name__ == "__main__":
    main()
