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
    print("\n== studio ledgers ==")
    for seed in args.seeds:
        led = ledgers[("studio", seed)]
        print(seed, json.dumps({k: v for k, v in led.items() if k != "reviews"}))
        print("   reviews:", led["reviews"])
    if args.out:
        os.makedirs(args.out, exist_ok=True)
        frame.to_csv(os.path.join(args.out, "studio-parent-screen.csv"))
        with open(os.path.join(args.out, "studio-parent-ledgers.json"), "w", encoding="utf-8") as f:
            json.dump({f"{a}-{s}": v for (a, s), v in ledgers.items()}, f, indent=1, default=str)


if __name__ == "__main__":
    main()
