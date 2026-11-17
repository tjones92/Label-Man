"""Do Big-5 newcomers climb like pushed records or like regional breakouts? (LpGenreAllocationFollowup.md §55)
Usage from SimLogs/: python ../SimTools/analyze_newcomer_climb.py RUN [RUN ...]
History: every labelled Top 10 record 1961-64 (records_YEAR_final.csv), path read from hot100_weekly.csv 1960-64. A newcomer
is an act with no Hot 100 week before this record's debut (52+ weeks of lookback, so debuts from 1961 on). Sim: Top 10
records launched 1960-61 in each run; newcomer = NewSigning / Rising / Unsigned at launch. Per group: records, debut
position, weeks from debut to first Top 10, peak, and share debuting inside the Top 40."""
import re, sys
import numpy as np, pandas as pd

B5_HIST = {"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"}
B5_IDS = {f"label_000{i}" for i in range(1, 6)}
NEW = {"NewSigning", "Rising", "Unsigned"}
pd.set_option("display.width", 200)


def norm(s):
    s = re.split(r" with | and his | and her | featuring | & the ", str(s).lower())[0]
    return re.sub(r"[^a-z0-9]", "", s)


def ntitle(s):
    return re.sub(r"[^a-z0-9]", "", re.sub(r"\(.*?\)", "", str(s).lower()))


def summarise(df):
    return df.groupby("g").agg(records=("debut", "size"), debut=("debut", "median"), debut_mean=("debut", "mean"),
                               to_top10=("wk_to_t10", "median"), to_top10_mean=("wk_to_t10", "mean"),
                               debut_top40=("d40", "mean"), peak=("peak", "mean")).round(2)


h = pd.read_csv("historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
h["a"] = h.artist.map(norm); h["t"] = h.title.map(ntitle)
first_week = h.groupby("a").chartDate.min()
weeks = sorted(h.chartDate.unique())
wi = {d: i for i, d in enumerate(weeks)}
rows = []
for y in range(1961, 1965):
    rec = pd.read_csv(f"historical-top10-{y}/records_{y}_final.csv")
    for _, r in rec.iterrows():
        a, t = norm(r.artist), ntitle(r.title)
        p = h[(h.a == a) & (h.t == t)].sort_values("chartDate")
        if p.empty: continue
        d0 = p.chartDate.iloc[0]
        if d0.year < 1961 or d0.year != y and (p.position <= 10).idxmax() is None: continue
        t10 = p[p.position <= 10]
        if t10.empty or t10.chartDate.iloc[0].year != y: continue
        rows.append(dict(year=y, b5=r.label in B5_HIST, new=first_week.get(a) == d0, debut=p.position.iloc[0],
                         wk_to_t10=wi[t10.chartDate.iloc[0]] - wi[d0], peak=p.position.min(), d40=p.position.iloc[0] <= 40))
H = pd.DataFrame(rows).drop_duplicates()
H["g"] = np.where(H.b5, "Big5", "other") + np.where(H.new, " newcomer", " established")
print("HISTORY, Top 10 records debuting 1961-64")
print(summarise(H).to_string())

S = []
for run in sys.argv[1:]:
    r = pd.read_csv(f"{run}-records.csv", usecols=["week", "recordId", "labelId", "currentPosition"])
    a = pd.read_csv(f"{run}-single-launch-attribution.csv", usecols=["singleId", "year", "career"])
    c = r[r.currentPosition.between(1, 100)].sort_values("week")
    first = c.groupby("recordId").first()
    t10 = c[c.currentPosition <= 10].groupby("recordId").week.min()
    pk = c.groupby("recordId").currentPosition.min()
    first = first[first.index.isin(t10.index) & (first.week > 0)]
    first = first.join(a.set_index("singleId"), how="inner")
    for rid, f in first.iterrows():
        S.append(dict(b5=f.labelId in B5_IDS, new=f.career in NEW, debut=f.currentPosition, wk_to_t10=t10[rid] - f.week,
                      peak=pk[rid], d40=f.currentPosition <= 40))
if S:
    S = pd.DataFrame(S)
    S["g"] = np.where(S.b5, "Big5", "other") + np.where(S.new, " newcomer", " established")
    print(f"\nSIM, Top 10 records launched in run ({len(sys.argv) - 1} runs pooled)")
    print(summarise(S).to_string())
