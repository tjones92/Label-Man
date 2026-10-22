"""Screen analyzer for --studio-parent-labels (FilmBroadcastLabelResearch.md) against its matched control.

    python SimTools/analyze_studio_parent.py --control ttx-v1-tax --studio spl-v1-studio --seeds 1001 2002 3003

Per seed and arm, from ChartAuditRunner output and the console log:

- Top 10 slots by year, grouped: Big-5 (label_0001-0005), studio labels (MGM, ABC-Paramount, Warner Bros.,
  United Artists, Dot, Colpix by name; only Colpix exists in the control), and the field (everyone else).
- The field by PRODUCING label: a Top 10 slot of a master that changed hands through AI_MASTER_DEAL (shop,
  pickup, distress) is credited to its originating seller instead of the label selling it that week.
- Labels with a Top 10 record in a year, and first-time Top 10 labels (first year in the run).
- The largest studio label's slots and rank in each year (the "never a top-3 firm" guard).
- Studio arm only: soundtrack routes, parent capital by purpose, reviews/closures, free-agent bids and master
  deals bought by studio labels.
- Guards from analyze_offchart_screen.screen: unit shares, closures by starting tier, Major share incl. off-chart.

Paired differences are studio minus control, per seed, with the mean and standard error across seeds. Three
seeds understate reshuffle variance (three-seed-paired-se-understates-reshuffle); read per-seed swings of a few
points against a mechanism ledger, not the SE alone.
"""
import argparse
import json
import os
import re
import statistics
from collections import Counter, defaultdict

import pandas as pd

from analyze_offchart_screen import ROOT, path, screen, unquote

STUDIO_NAMES = ("MGM", "ABC-Paramount", "Warner Bros.", "United Artists", "Dot", "Colpix")
BIG5 = {f"label_{i:04d}" for i in range(1, 6)}
YEARS = range(1960, 1965)

# Owner names in SimLogs/historical-top10-<year>/records_<year>_grouped.csv ("owned" column). Decca is MCA from 1962;
# Dot is filed under its owner Paramount and Colpix under Columbia Pictures-Screen Gems. Fox, Buena Vista and Reprise
# stay in the field: none has a template here.
HISTORY_BIG5 = {"Columbia", "RCA Victor", "EMI (Capitol)", "Decca", "MCA", "Mercury"}
HISTORY_STUDIO = {"MGM", "ABC-Paramount", "Warner Bros.", "United Artists", "Paramount", "Columbia Pictures-Screen Gems"}
# Field guard tolerance: twice the studio arm's seed SE in that year, never under 3 points (about 16 slot-weeks).
FIELD_TOLERANCE_FLOOR = 0.03
MIN_LABELS_WITH_TOP10 = 45
# Rank guard, relaxed to history's precedent: owner-grouped, MGM was 2nd in 1960 (56 slots) and ABC-Paramount 3rd in
# 1962 (32); no studio label was ever 1st, and one held a top-3 place in two of the five years.
MAX_STUDIO_TOP3_YEARS = 2
MAX_STUDIO_SHARE = 0.25


def history():
    """Top 10 slot shares by owner group and labels with a Top 10 record, 1960-64, from the reconstructions."""
    out, seen = {}, set()
    for year in YEARS:
        g = pd.read_csv(os.path.join(ROOT, f"historical-top10-{year}", f"records_{year}_grouped.csv"))
        total = g.slots.sum()
        big5, studio = g.slots[g.owned.isin(HISTORY_BIG5)].sum() / total, g.slots[g.owned.isin(HISTORY_STUDIO)].sum() / total
        labels = set(g.label)
        out[year] = {"big5": big5, "studio": studio, "field": 1 - big5 - studio,
                     "labels": len(labels), "firstTime": len(labels - seen)}
        seen |= labels
    return out


def field_guard(frame, seeds):
    """Field by PRODUCING label against history, year by year (replaces the §17l control-relative guard).

    Pass: the studio arm is no further from history than the control, and not below history by more than the
    tolerance. Kill: below history by more than the tolerance (the field squeezed past history), or further from
    history than the control by more than the tolerance. Breadth guards stay: labels with a Top 10 record must
    not fall under 45 in any seed-year, and first-time Top 10 labels must stay within two paired SE of the control.
    """
    hist, rows = history(), []
    for year in YEARS:
        col = f"top10_fieldByProducer_{year}"
        if col not in frame.columns:
            continue
        control = [float(frame.loc[("control", s), col]) for s in seeds]
        studio = [float(frame.loc[("studio", s), col]) for s in seeds]
        h, c, s = hist[year]["field"], statistics.mean(control), statistics.mean(studio)
        tol = max(FIELD_TOLERANCE_FLOOR, 2 * statistics.stdev(studio) / len(studio) ** .5)
        verdict = ("kill: below history" if s < h - tol else "kill: away from history" if abs(s - h) > abs(c - h) + tol
                   else "pass" if abs(s - h) <= abs(c - h) else "pass (within tolerance)")
        labels = [int(frame.loc[("studio", sd), f"labelsWithTop10_{year}"]) for sd in seeds]
        first = [float(frame.loc[("studio", sd), f"firstTimeTop10Labels_{year}"]) -
                 float(frame.loc[("control", sd), f"firstTimeTop10Labels_{year}"]) for sd in seeds]
        first_se = statistics.stdev(first) / len(first) ** .5
        breadth = "kill" if min(labels) < MIN_LABELS_WITH_TOP10 else "pass"
        first_verdict = "pass" if year == YEARS[0] or abs(statistics.mean(first)) <= 2 * max(first_se, 1.0) else "watch"
        rows.append({"year": year, "historyField": h, "controlField": c, "studioField": s, "tolerance": tol,
                     "fieldGuard": verdict, "historyLabels": hist[year]["labels"], "minStudioLabels": min(labels),
                     "labelsGuard": breadth, "firstTimeDiff": statistics.mean(first), "firstTimeSE": first_se,
                     "firstTimeGuard": first_verdict,
                     "historyBig5": hist[year]["big5"], "historyStudio": hist[year]["studio"]})
    return pd.DataFrame(rows).set_index("year")


def rank_guard(frame, seeds):
    """Per seed: a studio label may rank 2nd or 3rd by Top 10 slots, as MGM and ABC-Paramount did. Kill on a 1st place,
    on a studio label in the top 3 in more than two of the five years, or on studio labels above 25% in any year."""
    rows = []
    for seed in seeds:
        row = frame.loc[("studio", seed)]
        ranks = {y: row.get(f"bestStudioRank_{y}") for y in YEARS}
        firsts = [y for y, r in ranks.items() if pd.notna(r) and r == 1]
        top3 = [y for y, r in ranks.items() if pd.notna(r) and r <= 3]
        over = [y for y in YEARS if row.get(f"top10_studio_{y}", 0) > MAX_STUDIO_SHARE]
        verdict = ("kill: rank 1 in " + ",".join(map(str, firsts)) if firsts else
                   f"kill: top 3 in {len(top3)} years" if len(top3) > MAX_STUDIO_TOP3_YEARS else
                   "kill: above 25% in " + ",".join(map(str, over)) if over else "pass")
        rows.append({"seed": seed, "ranks": " ".join(f"{y}:{'-' if pd.isna(r) else int(r)}" for y, r in ranks.items()),
                     "top3Years": len(top3), "maxStudioShare": max(row.get(f"top10_studio_{y}", 0) for y in YEARS),
                     "rankGuard": verdict})
    return pd.DataFrame(rows).set_index("seed")


def console(tag, seed, weeks):
    name = f"{tag}-default-{seed}-{weeks}-console.log"
    for candidate in (os.path.join(ROOT, tag, name), os.path.join(ROOT, name)):
        if os.path.exists(candidate):
            with open(candidate, encoding="utf-8", errors="replace") as f:
                return f.read().splitlines()
    raise FileNotFoundError(name)


def fields(line):
    return dict(re.findall(r'(\w+)=("[^"]*"|\S+)', line))


def studio_ids(p):
    directory = pd.read_csv(p("label-directory"))
    names, ids = unquote(directory.labelName), unquote(directory.labelId)
    return {i for i, n in zip(ids, names) if any(n == s or n.startswith(s + " ") for s in STUDIO_NAMES)}


def measure(tag, seed, weeks):
    p = lambda stream: path(tag, seed, stream, weeks)
    r = screen(tag, seed, weeks)
    studios = studio_ids(p)
    log = console(tag, seed, weeks)
    origin = {}
    for line in log:
        if line.startswith("AI_MASTER_DEAL "):
            f = fields(line)
            origin.setdefault(f["record"], f["seller"])
    records = pd.read_csv(p("records"), usecols=["year", "recordId", "labelId", "currentPosition"])
    records["labelId"], records["recordId"] = unquote(records.labelId), unquote(records.recordId)
    top10 = records[(records.currentPosition > 0) & (records.currentPosition <= 10)].copy()
    top10["producer"] = [origin.get(rec, lab) for rec, lab in zip(top10.recordId, top10.labelId)]
    group = lambda ids: ids.map(lambda i: "big5" if i in BIG5 else "studio" if i in studios else "field")
    top10["group"], top10["producerGroup"] = group(top10.labelId), group(top10.producer)
    seen = set()
    for year in YEARS:
        slots = top10[top10.year == year]
        if slots.empty:
            continue
        for g in ("big5", "studio", "field"):
            r[f"top10_{g}_{year}"] = (slots.group == g).mean()
        r[f"top10_fieldByProducer_{year}"] = (slots.producerGroup == "field").mean()
        labels = set(slots.labelId)
        r[f"labelsWithTop10_{year}"] = len(labels)
        r[f"firstTimeTop10Labels_{year}"] = len(labels - seen)
        seen |= labels
        counts = slots.labelId.value_counts()
        studio_counts = counts[[i in studios for i in counts.index]]
        r[f"maxStudioSlots_{year}"] = int(studio_counts.max()) if len(studio_counts) else 0
        r[f"bestStudioRank_{year}"] = (int(list(counts.index).index(studio_counts.idxmax()) + 1)
                                       if len(studio_counts) else None)
    sales = pd.read_csv(p("label-sales-weekly"))
    sales = sales[unquote(sales.format) == "Single"]
    sales["labelId"] = unquote(sales.labelId)
    r["studioSingleShare"] = sales.units[sales.labelId.isin(studios)].sum() / sales.units.sum()
    r["big5SingleShare"] = sales.units[sales.labelId.isin(BIG5)].sum() / sales.units.sum()

    ledger = {"routes": Counter(), "capital": defaultdict(float), "bids": Counter(), "studioDeals": Counter(),
              "reviews": [], "closures": []}
    for line in log:
        head = line.split(" ", 1)[0]
        if head == "STUDIO_SOUNDTRACK_ROUTE":
            ledger["routes"][fields(line)["route"]] += 1
        elif head == "STUDIO_PARENT_CAPITAL":
            f = fields(line)
            ledger["capital"][f["purpose"]] += float(f["amount"])
        elif head == "STUDIO_FREE_AGENT_BID":
            ledger["bids"]["accepted" if fields(line)["accepted"] == "True" else "declined"] += 1
        elif head == "AI_MASTER_DEAL":
            f = fields(line)
            if f["buyer"] in studios:
                ledger["studioDeals"][f"{f['route']}-{f['kind']}"] += 1
        elif head == "STUDIO_PARENT_REVIEW":
            f = fields(line)
            ledger["reviews"].append((int(f["year"]), f["label"], float(f["drawn"]), int(f["lossYears"])))
        elif head == "STUDIO_PARENT_CLOSURE":
            ledger["closures"].append(fields(line)["id"])
    routed = sum(ledger["routes"].values())
    r["soundtrackOwnStudioShare"] = ledger["routes"]["own-studio"] / routed if routed else 0.0
    r["parentCapital(k)"] = sum(ledger["capital"].values()) / 1e3
    r["studioBidsAccepted"] = ledger["bids"]["accepted"]
    r["studioMasterDeals"] = sum(ledger["studioDeals"].values())
    r["studioClosures"] = len(ledger["closures"])
    ledger = {k: (dict(v) if isinstance(v, (Counter, defaultdict)) else v) for k, v in ledger.items()}
    return r, ledger


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--control", default="ttx-v1-tax")
    parser.add_argument("--studio", default="spl-v1-studio")
    parser.add_argument("--seeds", nargs="+", type=int, default=[1001, 2002, 3003])
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--out", default=None, help="write per-seed tables and ledgers here")
    args = parser.parse_args()
    rows, ledgers = {}, {}
    for arm, tag in (("control", args.control), ("studio", args.studio)):
        for seed in args.seeds:
            rows[(arm, seed)], ledgers[(arm, seed)] = measure(tag, seed, args.weeks)
    frame = pd.DataFrame(rows).T
    frame.index.names = ["arm", "seed"]
    pd.set_option("display.width", 250); pd.set_option("display.max_columns", 20); pd.set_option("display.max_rows", 400)
    print("== per seed ==")
    print(frame.T.to_string(float_format=lambda v: f"{v:.4f}"))
    print("\n== paired: studio - control (mean, SE, per seed) ==")
    for col in frame.columns:
        diffs = []
        for seed in args.seeds:
            a, b = frame.loc[("control", seed), col], frame.loc[("studio", seed), col]
            if pd.notna(a) and pd.notna(b):
                diffs.append(float(b) - float(a))
        if len(diffs) < 2:
            continue
        se = statistics.stdev(diffs) / len(diffs) ** .5
        print(f"{col:32s} {statistics.mean(diffs):+10.4f} ± {se:8.4f}   " + " ".join(f"{d:+.4f}" for d in diffs))
    guard = field_guard(frame, args.seeds)
    print("\n== field guard: by producing label vs history (shares) ==")
    print(guard.to_string(float_format=lambda v: f"{v:.3f}"))
    ranks = rank_guard(frame, args.seeds)
    print("\n== rank guard: best studio label's rank by Top 10 slots ==")
    print(ranks.to_string(float_format=lambda v: f"{v:.3f}"))
    print("\n== studio ledgers ==")
    for seed in args.seeds:
        led = ledgers[("studio", seed)]
        print(seed, json.dumps({k: v for k, v in led.items() if k != "reviews"}))
        print("   reviews:", led["reviews"])
    if args.out:
        os.makedirs(args.out, exist_ok=True)
        frame.to_csv(os.path.join(args.out, "studio-parent-screen.csv"))
        guard.to_csv(os.path.join(args.out, "studio-parent-field-guard.csv"))
        ranks.to_csv(os.path.join(args.out, "studio-parent-rank-guard.csv"))
        with open(os.path.join(args.out, "studio-parent-ledgers.json"), "w", encoding="utf-8") as f:
            json.dump({f"{a}-{s}": v for (a, s), v in ledgers.items()}, f, indent=1, default=str)


if __name__ == "__main__":
    main()
