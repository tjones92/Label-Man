"""Artist-evolution gate report for one run, against a baseline.

usage: python evolution_gate.py <run> [--baseline mix8-decade] [--genres Folk,FolkRock,Country]

Answers the four Gate-1 questions in one place:
  1. Total genre-share sumAbsErr vs targets, and the per-genre degradation against the
     baseline run (the gate is <= 320 total, and no benchmarked genre worse by > 4.0).
  2. Identity-holding releasing artists per year per genre, read from supply-selections --
     the same source that reproduces the directive's 1964-67 folk table exactly.
  3. The conversion ledger from <run>-artist-evolution.csv: who ratified, into what, and
     for the refusals, which guardrail said no.
  4. Country's identity population and share, which section 7.5 calls out specifically:
     FormationAffinity leaks into project transition, so ratification could turn a
     transient leak into permanent conversions into a genre mix8 just calibrated.

Diagnose from decision telemetry, not annual aggregates: when a genre moves, section 3
of this report is what says which conversions moved it.
"""
import argparse, csv, os, sys
from collections import Counter, defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import SIMLOGS, load_shape, load_targets, norm

ap = argparse.ArgumentParser()
ap.add_argument("run")
ap.add_argument("--baseline", default="mix8-decade")
ap.add_argument("--genres", default="Folk,ContemporaryFolk,FolkRock,Country,Soul")
a = ap.parse_args()

YEARS = range(1960, 1970)
watch = [g.strip() for g in a.genres.split(",") if g.strip()]


def sum_abs_err(run):
    shape, tg = load_shape(run), load_targets()
    per = defaultdict(float)
    for (y, g), target in tg.items():
        row = shape.get((y, g))
        if row is None:
            continue
        per[g] += abs(float(row["marketUnitsShare"]) * 100.0 - target)
    return per, sum(per.values())


def identity_counts(run):
    """year -> genre -> distinct artists releasing under that identity."""
    path = os.path.join(SIMLOGS, f"{run}-supply-selections.csv")
    if not os.path.exists(path):
        return None
    seen = defaultdict(lambda: defaultdict(set))
    with open(path, newline="") as f:
        for r in csv.DictReader(f):
            seen[int(r["year"])][r["artistIdentity"]].add(r["artistId"])
    return {y: {g: len(ids) for g, ids in per.items()} for y, per in seen.items()}


def conversions(run):
    path = os.path.join(SIMLOGS, f"{run}-artist-evolution.csv")
    if not os.path.exists(path):
        return None
    rows = list(csv.DictReader(open(path, newline="")))
    return rows


print(f"=== 1. genre share vs targets: {a.run} against {a.baseline} ===")
per_run, total_run = sum_abs_err(a.run)
per_base, total_base = sum_abs_err(a.baseline)
print(f"total sumAbsErr  run={total_run:.1f}  baseline={total_base:.1f}  delta={total_run - total_base:+.1f}")
worst = sorted(((per_run[g] - per_base.get(g, 0.0), g) for g in per_run), reverse=True)
print("largest per-genre degradations (gate: no benchmarked genre worse by > 4.0):")
for delta, g in worst[:8]:
    print(f"  {g:<22} {per_base.get(g, 0.0):7.2f} -> {per_run[g]:7.2f}   {delta:+.2f}")

print(f"\n=== 2. identity-holding releasing artists ===")
run_ids, base_ids = identity_counts(a.run), identity_counts(a.baseline)
if run_ids is None:
    print("  (no supply-selections.csv for this run)")
else:
    header = "  year  " + "".join(f"{g[:14]:>16}" for g in watch)
    print(header)
    for y in YEARS:
        cells = ""
        for g in watch:
            now = run_ids.get(y, {}).get(g, 0)
            was = (base_ids or {}).get(y, {}).get(g, 0)
            cells += f"{now:>10}{now - was:+6}"
        print(f"  {y}{cells}")

print(f"\n=== 3. conversion ledger ===")
rows = conversions(a.run)
if rows is None:
    print("  (no artist-evolution.csv -- run without --enable/--observe-artist-evolution)")
else:
    ratified = [r for r in rows if r["ratified"] == "true"]
    print(f"  candidate observations={len(rows)}  ratified={len(ratified)}  refused={len(rows) - len(ratified)}")
    print("  refusals by guardrail (a cap that binds everywhere means the rule is mistuned, not safe):")
    for block, n in Counter(r["block"] for r in rows if r["ratified"] != "true").most_common():
        print(f"    {block:<26} {n}")
    print("  conversions per year:")
    by_year = Counter(int(r["year"]) for r in ratified)
    for y in YEARS:
        print(f"    {y}  {by_year.get(y, 0)}")
    print("  top migrations (from -> to):")
    for (fr, to), n in Counter((r["fromGenre"], r["toGenre"]) for r in ratified).most_common(15):
        print(f"    {fr:<20} -> {to:<20} {n}")
    print("  triggers:")
    for trig, n in Counter(r["trigger"] for r in ratified).most_common():
        print(f"    {trig:<26} {n}")
    print("  net identity flow (into - out of):")
    flow = Counter()
    for r in ratified:
        flow[r["toGenre"]] += 1
        flow[r["fromGenre"]] -= 1
    for g, n in sorted(flow.items(), key=lambda kv: -kv[1]):
        if n:
            print(f"    {g:<22} {n:+}")
