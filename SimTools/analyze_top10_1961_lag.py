"""Diagnose the 1961 Top 10 concentration excess (HistoricalLabelTierBalanceResearch.md section 16).

Reads logs only: the five-year review runs and the Billboard reconstruction. Requires pandas.
CR4 is split as (top-four firms' Top 10 records) x (their weeks per Top 10 record) / 520, on the same
within-year basis for simulation and history. "Opening roster" means artists with no formation or
initial-reserve cohort, i.e. present on a label at the start of the run.
"""
import argparse
import json
import re
from pathlib import Path

import pandas as pd

BIG5_IDS = {f"label_000{i}" for i in range(1, 6)}
BIG5_NAMES = {"RCA Victor", "Columbia", "Capitol", "Decca", "Mercury"}
YEARS = range(1960, 1965)
LATER_COHORTS = {"RuntimeFormation", "EnabledInitialReserve"}


def artist_key(name):
    name = re.split(r"\s+(with|featuring|feat\.|and his|and her|and the|&|/)\s+", name.lower())[0]
    return re.sub(r"[^a-z0-9]", "", re.sub(r"^the\s+", "", name))


def head_split(slots_by_record):
    """slots_by_record: one row per (firm, record) with that record's Top 10 slots in the year."""
    firm = slots_by_record.groupby("firm").slots.sum().sort_values(ascending=False)
    head = slots_by_record.firm.isin(firm.index[:4])
    h, rest = slots_by_record[head], slots_by_record[~head]
    return {"cr4": round(100 * h.slots.sum() / 520, 2), "headRecords": len(h),
            "headWeeksPerRecord": round(h.slots.mean(), 2), "restWeeksPerRecord": round(rest.slots.mean(), 2),
            "allWeeksPerRecord": round(slots_by_record.slots.mean(), 2), "distinctRecords": len(slots_by_record)}


def history():
    out, weekly = {}, {}
    for year in YEARS:
        g = pd.read_csv(f"SimLogs/historical-top10-{year}/records_{year}_grouped.csv")
        split = head_split(g.rename(columns={"label": "firm"})[["firm", "slots"]])
        split["big5SlotSharePct"] = round(100 * g.loc[g.label.isin(BIG5_NAMES), "slots"].sum() / 520, 1)
        out[year] = split
        w = pd.read_csv(f"SimLogs/historical-top10-{year}/top10_weekly_{year}.csv")
        w = w[w.chartDate.isin(sorted(w.chartDate.unique())[-52:])]   # the file opens on the prior year's last chart
        weekly[year] = w.assign(artist=w.artist.map(artist_key))
    for year in list(YEARS)[1:]:
        cur, prev = weekly[year], weekly[year - 1]
        out[year]["slotsByPriorYearTop10ArtistsPct"] = round(100 * cur.artist.isin(set(prev.artist)).mean(), 1)
    return out


def simulation(prefix):
    r = pd.read_csv(prefix + "-records.csv", usecols=["week", "year", "recordId", "artistId", "labelId",
                                                       "currentPosition", "quality"])
    events = pd.read_csv(prefix + "-artist-population-events.csv", usecols=["artistId", "cohort"])
    cohort = events.drop_duplicates("artistId").set_index("artistId").cohort
    chart = r[r.currentPosition.between(1, 100)].copy()
    chart["opening"] = ~chart.artistId.map(cohort).isin(LATER_COHORTS)
    chart["big5"] = chart.labelId.isin(BIG5_IDS)
    top = chart[chart.currentPosition.le(10)]
    entries = chart.sort_values("week").drop_duplicates("recordId")
    out = {}
    for year in YEARS:
        t = top[top.year.eq(year)]
        per = t.groupby(["labelId", "recordId"]).size().rename("slots").reset_index().rename(columns={"labelId": "firm"})
        row = head_split(per)
        row["big5SlotSharePct"] = round(100 * t.big5.mean(), 1)
        e = entries[entries.year.eq(year) & entries.big5]
        row["big5Hot100EntriesOpeningRoster"] = int(e.opening.sum())
        row["big5Hot100EntriesLaterArtists"] = int((~e.opening).sum())
        row["openingRosterTop10SlotPct"] = round(100 * t.opening.mean(), 1)
        if year > 1960:
            row["slotsByPriorYearTop10ArtistsPct"] = round(
                100 * t.artistId.isin(set(top.loc[top.year.eq(year - 1), "artistId"])).mean(), 1)
        out[year] = row
    later = top[top.year.between(1961, 1963)]
    per = later.groupby(["year", "recordId"]).agg(weeks=("week", "size"), big5=("big5", "first"),
                                                  opening=("opening", "first"), quality=("quality", "first"))
    per["upperHalfQuality"] = per.quality.ge(per.quality.median())
    runs = {f"{key}={value}": round(float(w), 2) for key in ("big5", "opening")
            for value, w in per.groupby(key).weeks.mean().items()}
    runs.update({f"big5={b},upperHalfQuality={q}": round(float(w), 2)
                 for (b, q), w in per.groupby(["big5", "upperHalfQuality"]).weeks.mean().items()})
    return {"prefix": prefix, "annual": out, "top10WeeksPerRecord1961to63": runs}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    result = {"history": history(), "runs": [simulation(prefix) for prefix in args.prefixes]}
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    keys = ["cr4", "headRecords", "headWeeksPerRecord", "restWeeksPerRecord", "big5SlotSharePct",
            "slotsByPriorYearTop10ArtistsPct"]
    print("history  " + " | ".join(f"{y}: " + " ".join(str(result["history"][y].get(k, "-")) for k in keys)
                                   for y in YEARS))
    for run in result["runs"]:
        print(Path(run["prefix"]).name)
        for year, row in run["annual"].items():
            print(f"  {year}: " + " ".join(f"{k}={row.get(k, '-')}" for k in keys) +
                  f" big5Hot100(opening/later)={row['big5Hot100EntriesOpeningRoster']}/{row['big5Hot100EntriesLaterArtists']}")
        print("  1961-63 Top 10 weeks per record:", run["top10WeeksPerRecord1961to63"])


if __name__ == "__main__":
    main()
