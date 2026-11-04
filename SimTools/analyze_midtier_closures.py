"""Per-label 1960 outcome table for the starting MidTiers of a 53-week audit world.

Usage: python SimTools/analyze_midtier_closures.py SimLogs/<prefix> [<prefix> ...]
where <prefix> is e.g. SimLogs/slate-screen-v9-off-control-default-1001-53.
Prints one row per starting MidTier and a tier summary (closures by initial tier).
"""
import csv, os, sys, statistics as st
from collections import defaultdict

def rows(prefix, name):
    with open(f"{prefix}-{name}.csv", newline="", encoding="utf-8") as f:
        yield from csv.DictReader(f)

def f(x):
    try: return float(x)
    except (TypeError, ValueError): return 0.0

def analyse(prefix, verbose=True):
    directory = {r["labelId"]: r for r in rows(prefix, "label-directory")}
    fin = defaultdict(list)
    for r in rows(prefix, "label-finance"): fin[r["labelId"]].append(r)
    roster = defaultdict(dict)
    for r in rows(prefix, "label-scouting-vacancy-weekly"): roster[r["labelId"]][int(r["week"])] = int(f(r["rosterSize"]))
    opening = defaultdict(int); launches = defaultdict(int); launches_all = defaultdict(int)
    for r in rows(prefix, "demand-release-census"):
        if r["observationKind"] == "Opening": opening[r["labelId"]] += 1
        else:
            launches_all[r["labelId"]] += 1
            if int(f(r["observedAuditWeek"])) <= 13: launches[r["labelId"]] += 1
    emp = {}
    for r in rows(prefix, "company-monthly"):
        if r["month"] == "1" and r["labelId"] not in emp: emp[r["labelId"]] = (f(r["employees"]), f(r["payroll"]))
    out = []
    for lid, d in directory.items():
        hist = fin.get(lid, [])
        if not hist: continue
        exit_rows = [r for r in hist if r["exitReason"] not in ("", "None")]
        exitw = int(exit_rows[0]["week"]) if exit_rows else None
        early = [f(r["weeklyGross"]) for r in hist if int(r["week"]) <= 13]
        last = hist[-1]
        acts0 = roster[lid].get(1, roster[lid].get(min(roster[lid]) if roster[lid] else 0, 0))
        distress = sum(r["status"] in ("Struggling", "Dying") for r in hist if int(r["week"]) <= 13)
        out.append(dict(id=lid, distress13=distress, launches=launches_all[lid], name=d["labelName"], hist=d["isHistorical"] == "true", tier=d["initialTier"],
            acts=acts0, opening=opening[lid], launches13=launches[lid], gross13=st.mean(early) if early else 0,
            fte=emp.get(lid, (0, 0))[0], payroll=emp.get(lid, (0, 0))[1], exit=exitw,
            reason=exit_rows[0]["exitReason"] if exit_rows else "", endcash=f(last["cashReserves"])))
    if verbose:
        print(f"\n{prefix}")
        print(f"{'label':28} acts open L13 gross13/wk   FTE payroll/mo  exit  endCash")
        for o in sorted((o for o in out if o["tier"] == "MidTier"), key=lambda o: (o["exit"] is None, o["exit"] or 0)):
            print(f"{o['name'][:28]:28} {o['acts']:4} {o['opening']:4} {o['launches13']:3} {o['gross13']:10.0f} {o['fte']:6.2f} {o['payroll']:10.0f} {str(o['exit'] or '-'):>5} {o['endcash']:9.0f} {o['reason']}")
    summary = {}
    for tier in ("Major", "MidTier", "Independent", "Boutique", "Small"):
        t = [o for o in out if o["tier"] == tier]
        summary[tier] = (len(t), sum(o["exit"] is not None for o in t))
    return out, summary




def top10(prefix, tiers):
    """1960 Top 10 slot-weeks: CR4 over operating labels, and Major / MidTier shares by initial tier."""
    import pandas as pd
    rec = pd.read_csv(f"{prefix}-records.csv", usecols=["year", "labelId", "currentPosition"])
    rec["labelId"] = rec.labelId.astype(str).str.strip('"')
    t = rec[(rec.currentPosition > 0) & (rec.currentPosition <= 10) & (rec.year == 1960)]
    counts = t.labelId.value_counts()
    tier = t.labelId.map(tiers).fillna("Runtime")
    return dict(cr4=counts.head(4).sum() / len(t), major=(tier == "Major").mean(), mid=(tier == "MidTier").mean(), slots=len(t))


def screen(prefixes):
    """One row per run: closures of starting firms by initial tier, MidTier payroll, Top 10 concentration."""
    for p in prefixes:
        out, s = analyse(p, verbose=False)
        tiers = {o["id"]: o["tier"] for o in out}
        mids = [o for o in out if o["tier"] == "MidTier"]
        top = top10(p, tiers)
        hist_alive = sorted(o["name"] for o in mids if o["hist"] and o["exit"] is None)
        print(f"{os.path.basename(p):46} " + " ".join(f"{k[:5]} {c:3}/{n:3}" for k, (n, c) in s.items())
              + f" | MidPay {st.mean(o['payroll'] for o in mids):6.0f} | T10 CR4 {top['cr4']:.3f} Maj {top['major']:.3f} Mid {top['mid']:.3f}"
              + f" | Mid distress13 {st.mean(o['distress13'] for o in mids)/13:.2f} launches {sum(o['launches'] for o in mids)}"
              + f" | histMid alive: {', '.join(hist_alive)}")


if __name__ == "__main__":
    if sys.argv[1] == "--screen": screen(sys.argv[2:])
    else:
        for p in sys.argv[1:]:
            _, s = analyse(p)
            print("closures by initial tier:", {k: f"{c}/{n}" for k, (n, c) in s.items()})
