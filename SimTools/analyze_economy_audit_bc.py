"""Economy audit B3-B7 and section C: compare a switch arm with its control.

    python -I SimTools/analyze_economy_audit_bc.py SimLogs/bcb3-1001-104 --control SimLogs/bcoff-1001-104 [--json out.json]
    python -I SimTools/analyze_economy_audit_bc.py SimLogs/bcoff-1001-104 --identical SimLogs/a5on-1001-104

--identical byte-compares every CSV the two runs share (off-is-inert check) and lists files only one run wrote.
"""
import argparse
import csv
import glob
import hashlib
import json
import os
import statistics
import sys
from collections import Counter, defaultdict

TIERS = ["Major", "MidTier", "Independent", "Boutique", "Small"]


def rows(prefix, name):
    path = f"{prefix}-{name}.csv"
    if not os.path.exists(path):
        return
    with open(path, newline="", encoding="utf-8") as handle:
        yield from csv.DictReader(handle)


def f(value):
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def identical(a, b):
    def files(prefix):
        base = os.path.basename(prefix) + "-"
        return {os.path.basename(p)[len(base):]: p for p in glob.glob(prefix + "-*.csv")}
    fa, fb = files(a), files(b)
    digest = lambda p: hashlib.sha256(open(p, "rb").read()).hexdigest()
    shared = sorted(set(fa) & set(fb))
    differ = [n for n in shared if digest(fa[n]) != digest(fb[n])]
    return {"shared": len(shared), "differ": differ, "onlyA": sorted(set(fa) - set(fb)), "onlyB": sorted(set(fb) - set(fa))}


def summarize(prefix):
    s = {}
    mix = Counter(r["strategy"] or r["chosenFormat"] for r in rows(prefix, "release-strategy"))
    s["strategy"] = dict(mix)
    s["breadth"] = {r["year"]: {k: int(r[k]) for k in ("hot100Acts", "top40Acts", "top10Acts", "albumChartActs")}
                    for r in rows(prefix, "chart-breadth")}
    s["concentration"] = {r["year"]: {k: round(f(r[k]), 4) for k in ("c4ChartShare", "majorFamilyChartShare", "indieFamilyChartShare", "firmsCharting")}
                          for r in rows(prefix, "concentration")}
    ladder = list(rows(prefix, "career-ladder"))
    if ladder:
        last = ladder[-1]
        s["ladderFinal"] = {k: int(last[k]) for k in ("Rising", "Established", "Star", "Superstar")}
        s["ladderMoves"] = {k: sum(int(r[k]) for r in ladder) for k in ("toRising", "toEstablished", "toStar", "toSuperstar", "starToEstablished")}

    finance = list(rows(prefix, "label-finance"))
    if finance:
        final_week = max(int(r["week"]) for r in finance)
        final = [r for r in finance if int(r["week"]) == final_week]
        s["labelsFinal"] = {
            "defunct": sum(r["status"] == "Defunct" for r in final),
            "activeByTier": dict(Counter(r["labelTier"] for r in final if r["status"] != "Defunct")),
            "medianCashByTier": {t: round(statistics.median([f(r["cashReserves"]) for r in final if r["labelTier"] == t and r["status"] != "Defunct"]), 0)
                                 for t in TIERS if any(r["labelTier"] == t and r["status"] != "Defunct" for r in final)},
            "dealBalanceOutstanding": round(sum(f(r["dealUnrecoupedAdvance"]) for r in final), 0),
        }
        flows = defaultdict(Counter)
        for r in finance:
            t = r["labelTier"]
            for k in ("weeklyGross", "weeklyRoyalty", "weeklyNet", "weeklySkim", "weeklyDistributionIncome"):
                flows[t][k] += f(r[k])
        s["flowsByTier"] = {t: {k: round(v, 0) for k, v in c.items()} for t, c in flows.items()}

    deals = list(rows(prefix, "deal-ledger"))
    s["deals"] = {
        "resolutions": dict(Counter(r["resolution"] for r in deals)),
        "advancesSigned": round(sum(f(r["advance"]) for r in deals if r["resolution"] == "Signed"), 0),
        "courtedAdvances": round(sum(f(r["advance"]) for r in deals if r["resolution"] == "Signed" and r["origin"] == "DistributorCourted"), 0),
    }
    recoup = defaultdict(Counter)
    for r in rows(prefix, "distribution-advance-recoupment"):
        recoup[r["kind"]]["n"] += 1
        recoup[r["kind"]]["amount"] += f(r["amount"])
    if recoup:
        s["advanceRecoupment"] = {k: {"n": int(c["n"]), "amount": round(c["amount"], 0)} for k, c in recoup.items()}
    camp = defaultdict(Counter)
    for r in rows(prefix, "record-campaigns"):
        camp[r["kind"]]["n"] += 1
        camp[r["kind"]]["spend"] += f(r["spend"])
        if r["kind"] == "FollowUp":
            camp["FollowUpByPosition"]["top10" if 0 < int(r["position"]) <= 10 else "11-40"] += 1
    if camp:
        s["campaigns"] = {k: {kk: round(vv, 0) for kk, vv in c.items()} for k, c in camp.items()}
    pub = Counter()
    for r in rows(prefix, "publishing-ledger"):
        for k in ("publishingKept", "publishingReceived", "publishingTransferredOut", "publishingLeaked"):
            pub[k] += f(r[k])
    s["publishing"] = {k: round(v, 0) for k, v in pub.items()}
    s["rollup"] = {r["year"]: {k: round(f(r[k]), 0) for k in ("singleUnits", "singleGross", "singleNet", "albumUnits", "albumGross", "albumNet")}
                   for r in rows(prefix, "decade-annual-rollup")}
    roster = defaultdict(Counter)
    for r in rows(prefix, "roster-lifecycle"):
        for k in ("firstTimeSignings", "reSignings", "dropsToFreeAgentPool"):
            roster[r["labelTier"]][k] += int(f(r[k]))
    s["roster"] = {t: dict(c) for t, c in roster.items()}
    return s


def delta(arm, control, path=""):
    """Leaf-wise arm/control pairs where they differ."""
    out = {}
    for k in sorted(set(arm) | set(control), key=str):
        a, c = arm.get(k), control.get(k)
        if isinstance(a, dict) or isinstance(c, dict):
            d = delta(a or {}, c or {})
            if d:
                out[k] = d
        elif a != c:
            out[k] = [c, a]
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument("run")
    p.add_argument("--control")
    p.add_argument("--identical")
    p.add_argument("--json")
    a = p.parse_args()
    if a.identical:
        result = identical(a.run, a.identical)
        print(json.dumps(result, indent=1))
        sys.exit(1 if result["differ"] else 0)
    arm = summarize(a.run)
    result = {"run": a.run, "summary": arm}
    if a.control:
        result["control"] = a.control
        result["delta_control_arm"] = delta(arm, summarize(a.control))
    text = json.dumps(result, indent=1)
    if a.json:
        open(a.json, "w", encoding="utf-8").write(text)
    print(json.dumps(result.get("delta_control_arm", arm), indent=1))


if __name__ == "__main__":
    main()
