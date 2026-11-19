"""The 1960 Big-5 Top 10 dip (Big5ChartOpportunityInvestigation.md "The 1960 dip"). Usage from the repo root:
python SimTools/analyze_big5_1960_dip.py ARM [ARM ...]      (ARM = e.g. frd60pk; reads SimLogs/ARM-{1001,2002,3003,4004}-sc-*)
Per arm, 1960 chart weeks 0-51 (history: the 52 charts from 1959-12-28):
(1) Big-5 / field Top 10 records, acts, weeks per record, records with 7+ and 10+ weeks, and history 1960-64 for the same;
(2) Big-5 slot share by quarter;
(3) opening careers (engine log OPENING_CAREER lines): hitmaker acts per holder tier and their 1960 slots;
(4) weeks per Top 10 record by genre;
(5) complete runs, grouped by the year a record first entered the Top 10 (no calendar truncation; history from the 1960-64
    weekly Hot 100, sim entries to week 90 that leave the chart by week 103): Top 10 weeks, weeks after the peak, weeks at No. 1,
    records with 10+ Top 10 weeks.
History's prior-hit classes are hand-coded (a pre-1960 pop Top 10 / Top 40 only / neither) and approximate."""
import os, re, sys
import pandas as pd

B5 = [f"label_000{i}" for i in range(1, 6)]   # never compare ids as strings
B5_HIST = ["Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"]
SEEDS = [1001, 2002, 3003, 4004]
PRIOR_T10 = {"Connie Francis", "Elvis Presley With The Jordanaires", "The Everly Brothers", "Bobby Rydell", "Jack Scott", "Johnny Horton",
             "Percy Faith And His Orchestra", "Paul Anka", "Dinah Washington & Brook Benton", "The Drifters", "Sam Cooke", "Steve Lawrence",
             "Jackie Wilson", "Connie Stevens", "Marty Robbins", "Frankie Avalon", "Ray Charles", "Jimmy Clanton", "Freddie Cannon",
             "Dion & The Belmonts", "Duane Eddy And The Rebels", "Bobby Darin", "The Platters", "Brook Benton",
             "The Browns Featuring Jim Edward Brown", "Conway Twitty", "Guy Mitchell", "Andy Williams", "Fats Domino", "Annette",
             "Neil Sedaka", "Paul Evans"}
PRIOR_T40 = {"Jim Reeves", "Chubby Checker", "Anita Bryant", "Ray Peterson", "Jerry Butler", "Marv Johnson"}


def gz(path):
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"


def depth(rec, act, b5, w):
    o = {}
    for k, d in rec.groupby(b5):
        o["Big5" if k else "field"] = {"records": len(d), "slots": d[w].sum(), "acts": d[act].nunique(), "weeks/record": d[w].mean(),
                                       "records 7+ wk": (d[w] >= 7).sum(), "records 10+ wk": (d[w] >= 10).sum()}
    return pd.DataFrame(o)


def history():
    print("== History: Top 10 records by year")
    for y in range(1960, 1965):
        p = "SimLogs/historical-1960-top10/records_1960_final.csv" if y == 1960 else f"SimLogs/historical-top10-{y}/records_{y}_final.csv"
        h = pd.read_csv(p).assign(b5=lambda d: d.label.isin(B5_HIST))
        d = depth(h, "artist", "b5", "slots")
        print(y, "  ".join(f"{c}: {d.loc['records', c]:.0f} rec, {d.loc['weeks/record', c]:.2f} wk, {d.loc['records 7+ wk', c]:.0f} at 7+" for c in d))
    h = pd.read_csv("SimLogs/historical-1960-top10/records_1960_final.csv")
    h["cls"] = h.artist.map(lambda a: "prior Top 10" if a in PRIOR_T10 else "prior Top 40" if a in PRIOR_T40 else "new")
    h["grp"] = h.label.map(lambda l: "Big5" if l in B5_HIST else "field")
    print("\n1960 by the act's pre-1960 record (hand-coded)")
    print(h.groupby(["grp", "cls"]).slots.agg(records="size", slots="sum", weeks_per_record="mean").round(2).to_string())
    w = pd.read_csv("SimLogs/historical-1960-top10/top10_weekly_1960.csv").merge(h[["title", "artist", "label"]], on=["title", "artist"], how="left")
    dates = sorted(w.chartDate.unique())[:52]
    w = w[w.chartDate.isin(dates)]
    w["q"] = w.chartDate.map({d: i // 13 for i, d in enumerate(dates)})
    print("\nBig-5 share by quarter:", (100 * w.groupby("q").label.apply(lambda s: s.isin(B5_HIST).mean())).round(1).to_dict())


def runs(pos, rid, week, b5):
    """Per record with a Top 10 week: entry week, Top 10 weeks, weeks after the peak inside the Top 10, weeks at No. 1."""
    d = pd.DataFrame({"rid": rid, "w": week, "p": pos, "b5": b5})
    g = d.groupby("rid")
    o = pd.DataFrame({"first": g.w.min(), "last": g.w.max(), "peak": g.p.min(), "b5": g.b5.first()})
    t = d[d.p <= 10]
    o["entry"] = t.groupby("rid").w.min()
    o = o[o.entry.notna()].copy()
    o["t10"] = t.groupby("rid").size()
    o["n1"] = d[d.p == 1].groupby("rid").size().reindex(o.index).fillna(0)
    pw = d.merge(o.peak.rename("pk"), left_on="rid", right_index=True).query("p == pk").groupby("rid").w.min()
    o["after"] = t.merge(pw.rename("pw"), left_on="rid", right_index=True).query("w > pw").groupby("rid").size().reindex(o.index).fillna(0)
    return o


def complete_runs_table(o, years):
    return o.groupby([years, "b5"]).agg(records=("t10", "size"), t10_weeks=("t10", "mean"), after_peak=("after", "mean"),
                                        no1_weeks=("n1", "mean"), records_10plus=("t10", lambda x: (x >= 10).sum()))


def history_complete_runs():
    def key(t, a):
        a = re.split(r" with | and his | and her | featuring | & the ", str(a).lower())[0]
        return re.sub(r"[^a-z0-9]", "", str(t).lower()) + "|" + re.sub(r"[^a-z0-9]", "", a)
    labels = {}
    for y in range(1960, 1965):
        p = "SimLogs/historical-1960-top10/records_1960_final.csv" if y == 1960 else f"SimLogs/historical-top10-{y}/records_{y}_final.csv"
        for t, a, lab in pd.read_csv(p)[["title", "artist", "label"]].values:
            labels.setdefault(key(t, a), lab)
    h = pd.read_csv("SimLogs/historical-hot100-1960-64/hot100_weekly.csv")
    dates = sorted(h.chartDate.unique())
    h["k"] = [key(t, a) for t, a in zip(h.title, h.artist)]
    o = runs(h.position.values, h.k.values, h.chartDate.map({d: i for i, d in enumerate(dates)}).values,
             h.k.map(lambda k: labels.get(k) in B5_HIST).values)
    o = o[(o["first"] > 0) & (o["last"] < len(dates) - 1)]   # complete runs only
    print("\n== History: complete Top 10 runs by entry year (True = Big-5)")
    print(complete_runs_table(o, 1960 + o.entry // 52).round(2).to_string())


def sim_complete_runs(name):
    out = []
    for s in SEEDS:
        r = pd.read_csv(gz(f"SimLogs/{name}-{s}-sc-records.csv"), usecols=["week", "recordId", "labelId", "currentPosition"])
        r = r[r.currentPosition.between(1, 100)]
        o = runs(r.currentPosition.values, r.recordId.values, r.week.values, r.labelId.isin(B5).values)
        out.append(o[(o["first"] > 0) & (o["last"] < 104) & (o.entry <= 90)])
    o = pd.concat(out)
    t = complete_runs_table(o, 1960 + o.entry // 52)
    t[["records", "records_10plus"]] /= len(SEEDS)
    print(f"\n== {name}: complete Top 10 runs by entry year, per seed (True = Big-5; 1961 entries to week 90)")
    print(t.round(2).to_string())


def opening(run):
    rows = []
    for line in open(f"SimLogs/writing-calibration/{run}-engine.log", encoding="utf-8", errors="ignore"):
        m = re.search(r"OPENING_CAREER artist=(\S+) label=(\S+) tier=(\S+) .*archetype=(\S+) rung=(\S+)", line)
        if m:
            rows.append(dict(zip(["artistId", "ocLabel", "ocTier", "archetype", "rung"], m.groups())))
    return pd.DataFrame(rows).drop_duplicates("artistId")


def arm(name):
    print(f"\n== {name}: 1960 (weeks 0-51), per seed")
    D, Q, O, G = [], [], [], []
    for s in SEEDS:
        run = f"{name}-{s}-sc"
        r = pd.read_csv(gz(f"SimLogs/{run}-records.csv"), usecols=["week", "recordId", "artistId", "labelId", "genre", "currentPosition"])
        t = r[r.week.between(0, 51) & r.currentPosition.between(1, 10)].assign(b5=lambda d: d.labelId.isin(B5))
        rec = t.groupby(["recordId", "artistId", "b5", "genre"]).size().rename("w").reset_index()
        D.append(depth(rec, "artistId", "b5", "w"))
        Q.append(100 * t.groupby(t.week // 13).b5.mean())
        G.append(rec)
        oc = opening(run)
        oc = oc[oc.archetype.isin(["AdultStandardBearer", "CurrentHitmaker", "EstablishedHitmaker"])]
        oc["holder"] = oc.ocTier.where(~oc.ocLabel.isin(B5), "Big5")
        oc["slots"] = oc.artistId.map(t.groupby("artistId").size()).fillna(0)
        O.append(oc)
    print((sum(D) / len(SEEDS)).round(2).to_string())
    print("Big-5 share by quarter:", (sum(Q) / len(SEEDS)).round(1).to_dict())
    o = pd.concat(O).groupby(["holder", "archetype"]).agg(acts=("artistId", "size"), slots=("slots", "sum")) / len(SEEDS)
    print("\nHitmaker opening careers and their 1960 Top 10 slots")
    print(o.round(2).to_string())
    g = pd.concat(G).groupby(["genre", "b5"]).w.agg(["size", "mean"]).unstack("b5")
    g["size"] /= len(SEEDS)
    print("\nTop 10 records per seed and weeks per record, by genre (False = field)")
    print(g.round(2).to_string())


history()
history_complete_runs()
for a in sys.argv[1:]:
    arm(a)
    sim_complete_runs(a)
