"""Reconcile historical chart debuts, simulated launches and cross-market radio offers.

Run from any directory with pandas installed. Inputs are the archived SimLogs CSVs.
"""
from pathlib import Path
from math import exp
import re

import pandas as pd
import keyed_fit_draws as fit


LOG = Path(__file__).resolve().parents[1] / "SimLogs"
SEEDS = (1001, 2002, 3003, 4004)
FIRMS = {"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"}
IDS = {f"label_000{i}" for i in range(1, 6)}
NEW = {"Unsigned", "NewSigning"}


def source(run, name):
    path = LOG / f"{run}-{name}"
    return path if path.exists() else Path(str(path) + ".gz")


def artist_key(name):
    name = re.split(r" with | and his | and her | featuring | & the ", name.lower())[0]
    return re.sub(r"[^a-z0-9]", "", name)


def title_key(title):
    return re.sub(r"[^a-z0-9]", "", title.lower())


def history():
    weekly = pd.read_csv(LOG / "historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
    weekly["act"] = weekly.artist.map(artist_key)
    weekly["titleKey"] = weekly.title.map(title_key)
    weekly = weekly.sort_values("chartDate")
    keys = ["act", "titleKey"]
    records = weekly.groupby(keys).chartDate.min().rename("debut").to_frame()
    first_hit = weekly[weekly.position <= 10].groupby(keys).chartDate.min().rename("firstHit")
    hits = records.join(first_hit, how="inner").reset_index()
    hits["firstAct"] = hits.debut.eq(hits.act.map(weekly.groupby("act").chartDate.min()))
    for year in range(1960, 1965):
        directory = "historical-1960-top10" if year == 1960 else f"historical-top10-{year}"
        labels = pd.read_csv(LOG / directory / f"records_{year}_final.csv")
        labels["act"] = labels.artist.map(artist_key)
        labels["titleKey"] = labels.title.map(title_key)
        cohort = hits[hits.firstHit.dt.year.eq(year)].merge(labels[keys + ["label"]], on=keys,
                how="left", validate="one_to_one", indicator=True)
        unmatched = cohort[cohort._merge.ne("both") | cohort.label.isna()]
        if len(unmatched):
            raise ValueError(f"Missing labels in {year}: {unmatched[keys].to_dict('records')}")
        cohort["big"] = cohort.label.isin(FIRMS)
        same = cohort[cohort.debut.dt.year.eq(year)]
        debut_hits = same[same.firstAct]
        if year == 1961:
            print("Historical Big-5 first-act records", debut_hits.loc[debut_hits.big, keys].to_dict("records"))
        print("Historical", year, "calendar slots B/all", labels.loc[labels.label.isin(FIRMS), "slots"].sum(),
              labels.slots.sum(), "first T10 B/all", cohort.big.sum(), len(cohort),
              "same-year H100 debut then first T10 B/all", same.big.sum(), len(same),
              "first-act records B/all", debut_hits.big.sum(), len(debut_hits),
              "first-act counts incl prior-year debuts B/all", cohort[cohort.firstAct].big.sum(), cohort.firstAct.sum())


def simulated(run):
    a = pd.read_csv(source(run, "single-launch-attribution.csv"),
                    usecols=["week", "year", "singleId", "labelId", "artistId", "career"]).set_index("singleId")
    assert a.index.is_unique
    a["big"] = a.labelId.isin(IDS)
    a["new"] = a.career.isin(NEW)
    r = pd.read_csv(source(run, "records.csv"),
                    usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition"])
    c = r[r.currentPosition.between(1, 100)].sort_values("week")
    assert c.week.max() == r.week.max()  # charts publish every week of the run
    g = c.groupby("recordId")
    titles = g.first()[["week", "year", "artistId", "labelId"]].rename(columns={"week": "debut", "year": "debutYear"})
    titles["firstAct"] = titles.debut.eq(titles.artistId.map(c.groupby("artistId").week.min()))
    titles["big"] = titles.labelId.isin(IDS)
    c10 = c[c.currentPosition <= 10]
    t40 = c[c.currentPosition <= 40].groupby("recordId").week.min()
    t10 = c10.groupby("recordId").first()
    titles = titles.join(t10.year.rename("hitYear")).join(a.new)
    h61 = titles[titles.hitYear.eq(1961) & titles.debutYear.eq(1961)]
    print(run, "1961 first T10 B/all", titles[titles.hitYear.eq(1961)].big.sum(), titles.hitYear.eq(1961).sum(),
          "1961 H100 debuts B/all", titles[titles.debutYear.eq(1961)].big.sum(), titles.debutYear.eq(1961).sum(),
          "1961 first-act records B/all", h61[h61.firstAct].big.sum(), h61.firstAct.sum(),
          "1961 Big-5 chart-first x career", h61[h61.big].groupby(["firstAct", "new"]).size().to_dict(),
          "1961 B/all slots", c10[c10.year.eq(1961)].labelId.isin(IDS).sum(), c10.year.eq(1961).sum())
    a["debut"] = a.index.map(titles.debut)
    a["t40"] = a.index.map(t40)
    a["t10"] = a.index.map(t10.week)
    a["run"] = run
    return a


def launches(frames):
    a = pd.concat(frames)
    a = a[a.week.between(1, 92)].copy()
    # Same follow-up through launch +12 for every title, including chart entry at launch.
    for col in ["debut", "t40", "t10"]:
        age = a[col] - a.week
        assert not age.lt(0).any()
        a[col] = age.le(12)
    for name, cohort in [("All", a), ("NewSigning/Unsigned", a[a.new]), ("Other careers", a[~a.new])]:
        for split in [["big"], ["run", "big"]]:
            print(name, cohort.groupby(split).agg(launches=("week", "size"), H100=("debut", "sum"),
                  T40=("t40", "sum"), T10=("t10", "sum")).to_string())
    field = a[~a.big]
    major = a[a.big]
    field_at_major_mix = major.new.map(field.groupby("new").t10.mean()).mean()
    print("Field T10 at Big-5 career mix", field_at_major_mix, "mix factor", field_at_major_mix / field.t10.mean(),
          "remaining within-mix premium", major.t10.mean() / field_at_major_mix)


def auditions(run, a):
    offers = pd.read_json(source(run, "response-auditions.jsonl"), lines=True)
    meetings = ["week", "station"]
    occupied = offers.loc[offers.seat.eq("research-trial"), meetings].drop_duplicates()
    offers = offers[offers.offer].join(a[["week", "debut", "big", "new", "t10"]].rename(columns={"week": "launch"}),
                                      on="record", how="inner")
    early = offers[offers.new & offers.launch.le(92) & offers.week.le(offers.launch + 12)
                   & (offers.debut.isna() | offers.week.lt(offers.debut))].copy()
    assert early.week.ge(early.launch).all()
    early["arrival"] = early.rejection.eq("awaiting-arrival")
    early["city"] = early.rejection.eq("city-not-reached")
    early["geo"] = early.arrival | early.city
    early["start"] = early.seat.eq("research-trial")
    early["strong"] = early.sourceScore.ge(100)
    early["hit12"] = early.t10.le(early.launch + 12)
    early = early.merge(occupied.assign(occupied=True), on=meetings, how="left", validate="many_to_one")
    early["vacant"] = early.occupied.isna()
    print(run, "pre-entry offers", early.groupby("big").agg(offers=("record", "size"), records=("record", "nunique"),
          arrival=("arrival", "sum"), city=("city", "sum"), starts=("start", "sum")).to_dict("index"))
    return early.assign(run=run)


def regional(seed):
    run = f"nctrace-{seed}-sc"
    cols = ["week", "recordId", "region", "age", "labelId", "fitExposureAge", "awaitingArrival",
            "awareBuyers", "rawDemand", "cleared", "fitExposureCause"]
    t = pd.read_csv(source(run, "newcomer-connect-trace.csv"), usecols=cols)
    first = t[t.age.eq(1)].copy()
    assert first.groupby("recordId").size().eq(7).all()
    first["big"] = first.labelId.isin(IDS)
    first["exposed"] = first.fitExposureAge.eq(0)
    first["arrived"] = first.awaitingArrival.eq(0)
    first["per1000"] = 1000 * first.rawDemand / first.awareBuyers.where(first.awareBuyers.gt(0))
    fit.configure(*fit.params_for_run(run))
    at_kink = exp(fit.national_log(fit.KINK, .2) - fit.UNEXPOSED_CAP)
    print(run, "fit multiplier ratio at hit kink, same record and region, no prior hit", at_kink)
    # Compare regions of the very same record that have physically arrived but differ on fit exposure.
    cells = first[first.arrived].groupby(["big", "recordId", "exposed"]).per1000.mean().unstack().dropna()
    cells = cells.loc[cells[False].gt(0)]
    cells["ratio"] = cells[True] / cells[False]
    print(run, "paired first-week raw units per thousand aware buyers",
          cells.groupby(level="big").ratio.agg(["size", "median"]).to_string())
    print(run, "first-week market states", first.groupby(["big", "arrived", "exposed"]).agg(
        regions=("region", "size"), medianAware=("awareBuyers", "median"), medianSales=("cleared", "median")).to_string())
    # An exposure scheduled for next week has not yet affected this week's conversion.
    keys = ["recordId", "region"]
    closed = first[first.week.le(92) & ~first.exposed].set_index(keys)
    opened = (t[t.fitExposureAge.gt(0) & t.age.ge(t.fitExposureAge)]
              .sort_values("age").drop_duplicates(keys).set_index(keys))
    closed["openedBy"] = opened.fitExposureCause.reindex(closed.index).fillna("no observed opening by age 12")
    print(run, "initially fit-capped regions by first effective opening",
          closed.groupby(["big", "openedBy"]).size().to_string())


def main():
    history()
    frames, offers = [], []
    for seed in SEEDS:
        run = f"zfg-{seed}-sc"
        a = simulated(run)
        frames.append(a)
        offers.append(auditions(run, a))
        m = pd.read_csv(source(run, "market-clearing-weekly.csv"))
        print(run, "cleared/serviceable", m.clearedSingleUnits.sum() / m.serviceableSingleIntent.sum(),
              "cleared/raw", m.clearedSingleUnits.sum() / m.rawSingleDemand.sum())
    launches(frames)
    o = pd.concat(offers)
    by_record = o.groupby(["run", "record", "big"]).agg(allGeo=("geo", "all"), anyGeo=("geo", "any"),
                    started=("start", "any"), hit=("hit12", "any")).reset_index()
    print("Pre-entry offered titles", by_record.groupby("big").agg(titles=("record", "size"),
          all_geo=("allGeo", "sum"), any_geo=("anyGeo", "sum"), started=("started", "sum")).to_string())
    print("Offered titles by group and all offers geo-blocked",
          by_record.groupby(["big", "allGeo"]).hit.agg(["sum", "size"]).to_string())
    print("Pre-entry offers with source score >=100", o[o.strong].groupby("big").agg(
        n=("record", "size"), geo=("geo", "sum"), started=("start", "sum")).to_string())
    # One reserved research seat per station. Only count a meeting once, however many offers it received.
    possible = o[o.city & o.vacant & o.strong & o.seat.isna()]
    print("Vacant trial seat, strong report, city veto after arrival", possible.groupby("run").agg(
        offers=("record", "size"), titles=("record", "nunique")).to_string())
    print("Distinct such station-weeks", len(possible.drop_duplicates(["run", "week", "station"])))
    print("Strongest such observed report", possible.nlargest(1, "sourceScore")[
        ["run", "week", "record", "station", "region", "sourceRegion", "sourceScore", "localUnits", "debut", "t10"]].to_string(index=False))
    for seed in (2002, 3003):
        regional(seed)


if __name__ == "__main__":
    main()
