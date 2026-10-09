"""Band-member simulation measures (SimTools/BandMemberSimulationDirective.md §7) from a run's lineup telemetry.

Usage:  py SimTools/analyze_band_life.py <run-name> [<run-name> ...]
Reads SimLogs/<run>-lineup-events.csv and -lineup-annual.csv and Data/LineupReferenceSet.csv; prints the
measures and writes SimLogs/<run>-lineup-measures.txt. Diagnostic output only (never committed).
"""
import csv
import os
import sys
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")
CAUSES = ["CreditAndMoney", "Spotlight", "Direction", "Reliability", "Romance", "Burnout", "Outsider"]


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f))


def reference():
    rows = read(os.path.join(ROOT, "Data", "LineupReferenceSet.csv"))
    groups = [r for r in rows if r["type"] != "Solo"]
    changed = [r for r in groups if r["lineupChange"] == "Y"]
    mix = Counter(r["changeCause"] for r in changed)
    service = sum(1 for r in rows if r["serviceLoss"] == "Y" and r["serviceYear"] and int(r["serviceYear"]) >= 1960)
    return {
        "groups": len(groups), "changedShare": len(changed) / max(1, len(groups)),
        "splitShare": sum(r["split"] == "Y" for r in groups) / max(1, len(groups)),
        "mix": mix, "serviceShare": service / max(1, len(rows)), "acts": len(rows),
    }


def analyze(run):
    events = read(os.path.join(LOGS, f"{run}-lineup-events.csv"))
    annual = read(os.path.join(LOGS, f"{run}-lineup-annual.csv"))
    out = []
    p = out.append
    ref = reference()
    p(f"=== {run} ===")
    years = [int(r["year"]) for r in annual]
    p(f"years {min(years)}-{max(years)}")

    departures = [e for e in events if e["event"] in ("departure", "departure-dissolves")]
    # WalkedOut is a strain exit rerouted by §16 (it keeps its Reliability cause); the other §16 kinds are life exits.
    strain_kinds = {"Acrimony", "Fired", "SoloCareer", "Dissolution", "WalkedOut"}
    strain_dep = [e for e in departures if e["kind"] in strain_kinds]
    life_dep = [e for e in departures if e["kind"] not in strain_kinds]

    # 1. churn by success tier: group acts losing a member, charted vs never charted; quiet vs dramatic failure
    acts_dep = defaultdict(list)
    for e in departures:
        acts_dep[e["artistId"]].append(e)
    charted_groups = max(int(r["chartedGroupActs"]) for r in annual)
    groups = max(int(r["groupActs"]) for r in annual)
    lost_charted = len({a for a, es in acts_dep.items() if any(x["everCharted"] == "true" and x["constitution"] != "Solo" for x in es)})
    lost_never = len({a for a, es in acts_dep.items() if any(x["everCharted"] == "false" and x["constitution"] != "Solo" for x in es)})
    quiet = Counter((e["everCharted"]) for e in events if e["event"] == "quiet-dissolution")
    dramatic_never = sum(1 for e in strain_dep if e["everCharted"] == "false")
    p("\n[1] churn by success tier")
    p(f"  group acts (peak in scope) {groups}, ever-charted groups (peak) {charted_groups}")
    p(f"  charted groups losing a member: {lost_charted} ({lost_charted / max(1, charted_groups):.0%})  ref changed-share {ref['changedShare']:.0%}")
    p(f"  never-charted groups losing a member: {lost_never} ({lost_never / max(1, groups - charted_groups):.0%})")
    p(f"  failing acts: quiet dissolutions {quiet.get('false', 0)} vs strain departures {dramatic_never}"
      f"  -> quiet share {quiet.get('false', 0) / max(1, quiet.get('false', 0) + dramatic_never):.0%}")
    concentrated = lost_charted / max(1, charted_groups) > 3 * lost_never / max(1, groups - charted_groups)
    p(f"  KILL if churn concentrated in successful acts while failing acts split dramatically: "
      f"{'CHECK' if concentrated and dramatic_never > quiet.get('false', 0) else 'ok'}")

    # 2. cause mix
    mix = Counter(e["cause"] for e in strain_dep)
    total = sum(mix.values())
    p("\n[2] cause mix of strain-driven departures (model) vs reference first changes")
    for c in CAUSES:
        p(f"  {c:15s} model {mix.get(c, 0):5d} ({mix.get(c, 0) / max(1, total):5.1%})   ref {ref['mix'].get(c, 0):3d}")
    life_mix = Counter(e["kind"] for e in life_dep)
    p(f"  life-event departures: {dict(life_mix)}")
    top = max(mix.values()) / max(1, total) if total else 0
    p(f"  pooled max {top:.1%} (report only: the reference covers top-40 groups, so the kill reads charted groups)")
    # The pooled mix is mostly never-charted acts, where Spotlight and Credit (fame- and money-scaled) are ~0 and
    # Reliability wins by default. Compare like with like: charted groups against the reference, by period.
    groups_dep = [e for e in strain_dep if e["constitution"] != "Solo"]
    for lo, hi in ((1960, 1964), (1965, 1966), (1967, 1969)):
        for tier in ("true", "false"):
            s = [e for e in groups_dep if lo <= int(e["year"]) <= hi and e["everCharted"] == tier]
            if s:
                c = Counter(e["cause"] for e in s)
                p(f"  {lo}-{hi} {'charted' if tier == 'true' else 'never  '} n={len(s):4d}  " +
                  "  ".join(f"{k[:5]} {c.get(k, 0) / len(s):4.0%}" for k in CAUSES))
    charted_dep = [e for e in groups_dep if e["everCharted"] == "true"]
    cmix = Counter(e["cause"] for e in charted_dep)
    ctop = max(cmix.values()) / len(charted_dep) if charted_dep else 0
    p(f"  KILL if any single cause > 45% of charted-group departures: {'FAIL' if ctop > 0.45 else 'ok'} (max {ctop:.1%})")

    # 3. cascades
    by_act = defaultdict(list)
    for e in departures:
        by_act[e["artistId"]].append(int(e["year"]))
    cascade = 0
    for ys in by_act.values():
        ys.sort()
        for i, y in enumerate(ys):
            if (i > 0 and y - ys[i - 1] <= 1) or (i + 1 < len(ys) and ys[i + 1] - y <= 1):
                cascade += 1
    p(f"\n[3] cascades: {cascade}/{len(departures)} departures within a year of another in the same act ({cascade / max(1, len(departures)):.0%})")
    breaker_years = sum(1 for r in annual if int(r["breakerDeferred"]) > 0)
    p(f"  breaker bound in {breaker_years} of {len(annual)} years (KILL if > 2)")

    # breaker sizing
    strain_by_year = {int(r["year"]): int(r["strainDepartures"]) for r in annual}
    p("\n[breaker] strain departures by year: " + ", ".join(f"{y}:{n}" for y, n in sorted(strain_by_year.items())))
    if strain_by_year:
        mean = sum(strain_by_year.values()) / len(strain_by_year)
        p(f"  mean {mean:.0f}/yr, max {max(strain_by_year.values())}; breaker at ~2x mean = {round(2 * mean)}")

    # 5a. draft
    p("\n[5a] draft by year (drafted / eligible, drafted from charted acts)")
    for r in annual:
        p(f"  {r['year']}: {r['drafted']:>4s} / {r['draftEligible']:>6s}  charted {r['draftedCharting']}")
    drafted_acts_charted = len({e["artistId"] for e in departures if e["kind"] == "Service" and e["everCharted"] == "true"})
    p(f"  charted acts losing a member to service: {drafted_acts_charted} / {charted_groups} groups (ref service share of acts {ref['serviceShare']:.1%})")

    # 5b. deaths
    p("\n[5b] deaths by year and channel (all / among ever-charted)")
    deaths = [e for e in departures if e["kind"] == "Death"]
    by = defaultdict(Counter)
    by_c = defaultdict(Counter)
    for e in deaths:
        by[int(e["year"])][e["channel"]] += 1
        if e["everCharted"] == "true":
            by_c[int(e["year"])][e["channel"]] += 1
    for y in sorted(set(years)):
        p(f"  {y}: all {dict(by[y])}   charted {dict(by_c[y])}")
    early_sub = sum(by_c[y]["Substance"] for y in by_c if y < 1966)
    early = sum(sum(by_c[y].values()) for y in by_c if y <= 1965)
    p(f"  charted deaths 1960-65: {early} (reference ~1-2/yr: 7 in 6 years)   charted substance deaths before 1966: {early_sub}")
    ages = sorted(int(e["age"]) for e in deaths if e["channel"] == "Substance")
    if ages:
        p(f"  substance-death ages: median {ages[len(ages) // 2]}, n={len(ages)}, histogram " +
          str(sorted(Counter((a // 5) * 5 for a in ages).items())))

    # stages and life events
    p("\n[stages/life] by year: brewing / ultimatum / strain departures / exhaustion / substance onsets / marriages / affairs / meanStrain / meanMorale")
    for r in annual:
        p(f"  {r['year']}: {r['brewing']:>4s} / {r['ultimatums']:>4s} / {r['strainDepartures']:>4s} / {r['exhaustion']:>4s} / "
          f"{r['substanceOnsets']:>4s} / {r['marriages']:>5s} / {r['affairs']:>3s} / {float(r['meanStrain']):.3f} / {float(r['meanMorale']):+.3f}")
    kinds = Counter(e["kind"] for e in departures)
    p(f"\nall departures by kind: {dict(kinds)}")
    # §16.4 measure 2: what the player reads. Share of each kind among never-charted group departures, by period.
    p("\n[16] never-charted group departures by kind and period (share of all their departures)")
    for lo, hi in ((1960, 1964), (1965, 1966), (1967, 1969)):
        es = [e for e in departures if e["everCharted"] == "false" and e["constitution"] != "Solo" and lo <= int(e["year"]) <= hi]
        if not es:
            continue
        k = Counter(e["kind"] for e in es)
        unreliable = sum(1 for e in es if e["kind"] == "Fired" and e["cause"] == "Reliability")
        p(f"  {lo}-{hi}: n={len(es)}  Fired(unreliable) {unreliable / len(es):.0%}  " +
          "  ".join(f"{kk} {v / len(es):.0%}" for kk, v in k.most_common()))
    text = "\n".join(out)
    print(text)
    with open(os.path.join(LOGS, f"{run}-lineup-measures.txt"), "w", encoding="utf-8") as f:
        f.write(text + "\n")


if __name__ == "__main__":
    for name in sys.argv[1:]:
        analyze(name)
