"""Head-scale template probe (HistoricalLabelTierBalanceResearch.md §17l): treatment against a paired control.

Reads two completed launcher manifests on one assembly. Reuses the formal CR4/CR8 review and chart guards from
analyze_top10_balance_probe, and adds the §17l guards: Big-5 share and top-four count, the field share (labels that
are neither Big-5 nor a §17l template), labels with a Top 10 hit, first-time Top 10 labels, non-major firms with 20+
slots, template ranks and shares, and template absorptions and tier moves.
Usage: python SimTools/analyze_head_templates_probe.py --control SimLogs/CTL/runs.json
       --candidate SimLogs/TAG/runs.json --output SimLogs/TAG/comparison.json
"""
import argparse
import json
from pathlib import Path
from statistics import mean, pstdev

import pandas as pd

from analyze_top10_balance_probe import GUARDS, load_configuration

BIG5 = {f"label_000{i}" for i in range(1, 6)}
TEMPLATES = ("MGM", "ABC-Paramount", "Warner Bros.", "Cameo-Parkway", "Liberty", "Vee-Jay")
HISTORY = {  # §17l table and §13: Big-5 operating share, first five §17l firms' share, labels with a Top 10 hit
    1960: (34.2, 23.3, 49), 1961: (20.6, 20.2, 51), 1962: (21.2, 24.6, 53), 1963: (26.0, 15.2, 54), 1964: (18.7, 6.7, 55)}


def template_ids(prefix):
    d = pd.read_csv(prefix + "-label-directory.csv")
    d = d[d.isHistorical.astype(str).str.lower() == "true"]
    out = {}
    for name in TEMPLATES:
        hit = d[(d.labelName == name) | d.labelName.str.startswith(name + " ")]
        if len(hit) == 1:
            out[name] = hit.labelId.iloc[0]
    return out


def template_metrics(prefix):
    ids = template_ids(prefix)
    tset = set(ids.values())
    chart = pd.read_csv(prefix + "-records.csv", usecols=["year", "week", "labelId", "currentPosition"])
    top = chart[chart.currentPosition.between(1, 10)]
    first_year = top.groupby("labelId").year.min()
    years = {}
    for year, t in top.groupby("year"):
        slots = t.groupby("labelId").size().sort_values(ascending=False)
        rank = {lab: i + 1 for i, lab in enumerate(slots.index)}
        years[int(year)] = dict(
            big5Pct=100 * slots[slots.index.isin(BIG5)].sum() / 520,
            big5InTop4=int(sum(l in BIG5 for l in slots.head(4).index)),
            templatePct=100 * slots[slots.index.isin(tset)].sum() / 520,
            fieldPct=100 * slots[~slots.index.isin(BIG5 | tset)].sum() / 520,
            labelsWithTop10=int(len(slots)),
            firstTimeTop10Labels=int((first_year == year).sum()),
            nonMajorFirms20Plus=int(((slots >= 20) & ~slots.index.isin(BIG5)).sum()),
            templates={name: dict(slots=int(slots.get(lid, 0)), rank=rank.get(lid)) for name, lid in ids.items()})
    tiers = pd.read_csv(prefix + "-label-tier-transitions.csv")
    tiers = tiers[tiers.labelId.isin(tset)]
    ledger = pd.read_csv(prefix + "-deal-ledger.csv")
    absorbed = ledger[ledger.clientId.isin(tset) & ledger.resolution.astype(str).str.contains("Absorb", case=False)]
    return dict(templateIds=ids, years=years,
                tierMoves=tiers[["year", "labelName", "direction", "fromTier", "toTier"]].to_dict("records"),
                absorptions=absorbed[["year", "clientName", "distributorName"]].to_dict("records"),
                distributedClients={name: int((ledger.distributorId == lid).sum()) for name, lid in ids.items()})


def summarize(config):
    runs = [dict(seed=r["seed"], **template_metrics(r["prefix"])) for r in config["runs"]]
    years = sorted(set.intersection(*(set(r["years"]) for r in runs)))
    keys = ("big5Pct", "big5InTop4", "templatePct", "fieldPct", "labelsWithTop10", "firstTimeTop10Labels", "nonMajorFirms20Plus")
    means = {y: {k: round(mean(r["years"][y][k] for r in runs), 2) for k in keys} for y in years}
    sds = {y: {k: round(pstdev([r["years"][y][k] for r in runs]), 2) for k in keys} for y in years}
    return runs, years, means, sds


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--control", required=True)
    p.add_argument("--candidate", required=True)
    p.add_argument("--output", required=True)
    a = p.parse_args()
    ctl, cand = load_configuration(a.control), load_configuration(a.candidate)
    if ctl["assemblySha256"] != cand["assemblySha256"] or ctl["seeds"] != cand["seeds"] or ctl["weeks"] != cand["weeks"]:
        raise ValueError("Control and candidate must share assembly, seeds and duration")
    out = dict(control=a.control, candidate=a.candidate, assemblySha256=cand["assemblySha256"], seeds=cand["seeds"])
    for label, cfg in (("control", ctl), ("candidate", cand)):
        runs, years, means, sds = summarize(cfg)
        out[label] = dict(annualMeans=cfg["annualMeans"], guardMeans=cfg["guardMeans"],
                          reviewStatus={y: cfg["reviews"][y]["status"] for y in cfg["reviews"]},
                          probeMeans=means, probeSds=sds, runs=runs)
    lines = ["year  arm        CR4    CR8   status        Big5%  B5top4 tmpl%  field%  labels firstT  nm20+"]
    for y in sorted(out["candidate"]["probeMeans"]):
        for arm in ("control", "candidate"):
            am, pm = out[arm]["annualMeans"][y], out[arm]["probeMeans"][y]
            lines.append(f"{y}  {arm:9s} {am['cr4']:6.2f} {am['cr8']:6.2f}  {am['status']:12s} {pm['big5Pct']:6.2f} {pm['big5InTop4']:6.2f} "
                         f"{pm['templatePct']:6.2f} {pm['fieldPct']:6.2f} {pm['labelsWithTop10']:6.1f} {pm['firstTimeTop10Labels']:6.1f} {pm['nonMajorFirms20Plus']:5.1f}")
        h = HISTORY.get(y)
        if h:
            lines.append(f"{y}  history                           {h[0]:6.2f}   1.75* {h[1]:6.2f}         {h[2]:6d}")
    lines.append("* 1961-64 mean")
    lines.append("guard            control   candidate")
    for k in GUARDS:
        lines.append(f"{k:24s} {out['control']['guardMeans'][k]:10.3f} {out['candidate']['guardMeans'][k]:10.3f}")
    lines.append("template slots/rank by seed-year (candidate):")
    for r in out["candidate"]["runs"]:
        for y in sorted(r["years"]):
            lines.append(f"  {r['seed']} {y}: " + "  ".join(f"{n} {v['slots']}/{v['rank'] or '-'}" for n, v in r["years"][y]["templates"].items()))
        lines.append(f"  {r['seed']} tierMoves={r['tierMoves']} absorptions={r['absorptions']} distributed={r['distributedClients']}")
    text = "\n".join(lines)
    out["text"] = text
    Path(a.output).write_text(json.dumps(out, indent=2, default=str), encoding="utf-8")
    Path(a.output).with_suffix(".txt").write_text(text, encoding="utf-8")
    print(text)


if __name__ == "__main__":
    main()
