"""First-year cash anatomy of the starting US MidTiers (SimTools/MidTierClosureResearch.md section 6).

Usage: python SimTools/analyze_midtier_first_year_cash.py <arm>=<53-week prefix>+<1-week fill prefix> ...
e.g. national=SimLogs/mtn-ab-national+SimLogs/mtn-fill-national. Prefixes omit -default-<seed>-<weeks>.
Launch acts come from the fill census (same seed and flags, so the same launch allocation). The shortfall is the
cash that would have kept every month-end at or above zero with unchanged flows: a static bound, since a firm
that stays out of Struggling releases more.
"""
import csv, statistics as st, sys
from collections import defaultdict

SEEDS = (1001, 2002, 3003)
LONDON = ("Parlophone", "Pye", "EMI", "Decca UK")
OUT = ("plantPayments", "recording", "campaigns", "artistAdvances", "licensing", "otherOperating", "payroll", "premises",
       "retainers", "ownerSalary", "taxPaid", "distributionOperations", "tradeSupport", "recordExcise")

def rows(path):
    with open(path, newline="", encoding="utf-8") as f: yield from csv.DictReader(f)

def f(x):
    try: return float(x)
    except (TypeError, ValueError): return 0.0

def firms(prefix, fill):
    out = []
    for s in SEEDS:
        p = f"{prefix}-default-{s}-53"
        d = {r["labelId"]: r for r in rows(f"{p}-label-directory.csv")}
        mids = {k for k, v in d.items() if v["initialTier"] == "MidTier" and not v["labelName"].startswith(LONDON)}
        acts = {r["labelId"]: int(r["filled"]) for r in rows(f"{fill}-default-{s}-1-scene-initialization-labels.csv")}
        months = defaultdict(list)
        for r in rows(f"{p}-company-monthly.csv"):
            if r["labelId"] in mids: months[r["labelId"]].append(r)
        exits = {}
        for r in rows(f"{p}-label-finance.csv"):
            if r["labelId"] in mids and r["exitReason"] not in ("", "None") and r["labelId"] not in exits: exits[r["labelId"]] = int(r["week"])
        for lid, ms in months.items():
            first = ms[0]; h1 = ms[:6]
            low = min(f(m["closingCash"]) for m in ms)
            out.append(dict(seed=s, name=d[lid]["labelName"], acts=acts.get(lid, 0), closed=lid in exits, open=f(first["openingCash"]),
                payroll=st.mean(f(m["payroll"]) for m in h1), receipts=st.mean(f(m["recordReceipts"]) + f(m["serviceIncome"]) for m in h1),
                spend=st.mean(sum(f(m[k]) for k in OUT) for m in h1), advances=sum(f(m["artistAdvances"]) for m in ms),
                recording=st.mean(f(m["recording"]) for m in h1), low=low, short=max(0.0, -low),
                lowmonth=min(ms, key=lambda m: f(m["closingCash"]))["month"]))
    return out

for arg in sys.argv[1:]:
    arm, prefixes = arg.split("=", 1)
    prefix, fill = prefixes.split("+")
    fs = firms(prefix, fill)
    print(f"\n== {arm}: {len(fs)} US MidTiers, {sum(x['closed'] for x in fs)} closed ==")
    for label, group in (("closed", [x for x in fs if x["closed"]]), ("survived", [x for x in fs if not x["closed"]])):
        if not group: continue
        m = lambda k: st.median(x[k] for x in group)
        print(f"{label:9} n={len(group):2} acts {m('acts'):.0f}  opening cash {m('open') / 1000:.0f}k  H1/mo: payroll {m('payroll') / 1000:.1f}k "
              f"spend {m('spend') / 1000:.1f}k receipts {m('receipts') / 1000:.1f}k  payroll/act {st.median(x['payroll'] / max(1, x['acts']) for x in group):.0f}  "
              f"low {m('low') / 1000:.0f}k  shortfall {m('short') / 1000:.0f}k (mean {st.mean(x['short'] for x in group) / 1000:.0f}k, max {max(x['short'] for x in group) / 1000:.0f}k)")
    print(f"{'firm':26} seed acts  open  pay/mo spend/mo rcpt/mo   low  lowM closed")
    for x in sorted(fs, key=lambda x: (x["closed"], x["low"])):
        print(f"{x['name'][:26]:26} {x['seed']} {x['acts']:4} {x['open'] / 1000:5.0f}k {x['payroll'] / 1000:5.1f}k {x['spend'] / 1000:7.1f}k {x['receipts'] / 1000:6.1f}k {x['low'] / 1000:6.0f}k {x['lowmonth']:>4} {x['closed']}")
