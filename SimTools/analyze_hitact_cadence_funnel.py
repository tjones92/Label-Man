"""Why field hit acts release fewer singles: the release-slate funnel per hit act (FollowUpGapFableHandoff.md, section 7).

usage: analyze_hitact_cadence_funnel.py name=fmt ...      ({s} = seed; seeds 2002, 1001)

Hit act and tier as in analyze_hitact_selection.py (first Top 10 in Y - 1, Y = 1961-63, the hit record's label; the
field split by that label's tier in Y - 1). Window: the 52 weeks after the hit's first Top 10 week, cut 12 weeks before
the run ends. Per hit act it reads release-slate.csv (one row per slot and state change) and single-launch-attribution.csv:

  1. singles launched in the window, how many came through a slate slot, weeks from the hit to the first launch, and
     whether the act was still at the hit's label at its last launch (departures);
  2. single slots that reach the window by source (hit-follow-up, roster-programme, contract-delivery) and how each
     ends (issued, cancelled with its last reason, still open);
  3. weeks a slot sat postponed, by reason, per hit act: where the follow-ups wait.
"""
import sys
import numpy as np, pandas as pd
from analyze_hitact_selection import SEEDS, sim

pd.set_option("display.width", 250)
ORDER = ["Big5", "MidTier", "Independent", "Small"]
WINDOW = 52


def load(fmt):
    h, a = sim(fmt)
    out = []
    for seed in SEEDS:
        run = "SimLogs/" + fmt.format(s=seed)
        wmax = pd.read_csv(run + "-records.csv", usecols=["week"]).week.max()
        s = pd.read_csv(run + "-release-slate.csv", usecols=["week", "slotId", "labelId", "artistId", "format", "state", "source", "reason", "recordId"])
        s = s[s.format == "Single"]
        hs = h[(h.seed == seed) & (h.hitWeek + WINDOW <= wmax - 12)]
        s = s[s.artistId.isin(hs.artistId)].merge(hs[["artistId", "hitWeek", "hitId", "fieldTier"]], on="artistId")
        s = s[(s.week > s.hitWeek) & (s.week <= s.hitWeek + WINDOW)]
        al = a[(a.seed == seed) & a.artistId.isin(hs.artistId)].merge(hs[["artistId", "hitWeek", "hitId"]], on="artistId")
        hitLabel = al[al.singleId == al.hitId].drop_duplicates("hitId").set_index("hitId").labelId
        al = al[(al.week > al.hitWeek) & (al.week <= al.hitWeek + WINDOW)]
        slotted = set(s.recordId.dropna())
        per = []
        for act in hs.itertuples():
            L = al[al.hitId == act.hitId].sort_values("week")
            S = s[s.hitId == act.hitId]
            last = S.sort_values("week").groupby("slotId").tail(1)
            post = S[S.state == "Postponed"].reason.value_counts()
            row = dict(seed=seed, artistId=act.artistId, tier=act.fieldTier, launches=len(L), viaSlot=int(L.singleId.isin(slotted).sum()),
                       firstGap=(L.week.iloc[0] - act.hitWeek) if len(L) else np.nan,
                       left=int(len(L) > 0 and L.labelId.iloc[-1] != hitLabel.get(act.hitId)),
                       noLaunch=int(len(L) == 0), slots=S.slotId.nunique())
            for src in ("hit-follow-up", "roster-programme", "contract-delivery"):
                row["slots:" + src] = last[last.source == src].shape[0]
            row["issued"] = int((last.state == "Issued").sum())
            row["cancelled"] = int((last.state == "Cancelled").sum())
            row["open"] = row["slots"] - row["issued"] - row["cancelled"]
            for reason, n in last[last.state == "Cancelled"].reason.value_counts().items():
                row["cancel:" + reason] = n
            for reason, n in post.items():
                row["wait:" + reason] = n
            per.append(row)
        out.append(pd.DataFrame(per))
    return pd.concat(out, ignore_index=True).fillna(0)


for name, fmt in [x.split("=", 1) for x in sys.argv[1:]]:
    d = load(fmt)
    g = d.groupby("tier")
    print(f"\n######## {name}  (hit acts with a full 52-week window after the hit's first Top 10)")
    t = pd.DataFrame({"acts": g.size(), "launches": g.launches.mean(), "viaSlot": g.viaSlot.mean(), "noLaunch %": 100 * g.noLaunch.mean(),
                      "firstGap med": g.firstGap.apply(lambda x: x[x > 0].median() if (x > 0).any() else np.nan),
                      "left label %": 100 * g.left.mean(), "slots": g.slots.mean(), "issued": g.issued.mean(), "cancelled": g.cancelled.mean(),
                      "open": g.open.mean()}).reindex(ORDER)
    print("1-2. per hit act, 52 weeks after the hit")
    print(t.round(2).to_string())
    src = [c for c in d.columns if c.startswith("slots:")]
    print(g[src].mean().reindex(ORDER).round(2).to_string())
    cancel = [c for c in d.columns if c.startswith("cancel:")]
    print("cancelled slots by last reason, per hit act")
    print(g[cancel].mean().reindex(ORDER).T.round(2).sort_values("Independent", ascending=False).head(10).to_string())
    wait = [c for c in d.columns if c.startswith("wait:")]
    print("3. postponed slot-weeks by reason, per hit act")
    print(g[wait].mean().reindex(ORDER).T.round(2).sort_values("Independent", ascending=False).head(12).to_string())
