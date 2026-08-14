"""returns-to-#1 metric from a run's weeks.csv.

A #1 SPELL is a maximal run of consecutive weeks with the same numberOneRecordId.
A spell is a RETURN if that recordId held #1 in an earlier, non-adjacent spell.
returns% = returns / total spells. Pre-fix baseline was 13.4% of spells.

usage: python returns_no1.py <run> [<run2> ...]
"""
import sys, csv, os
SIMLOGS = r"C:\Project\Label-Man\SimLogs"

def analyze(run):
    path = os.path.join(SIMLOGS, f"{run}-weeks.csv")
    if not os.path.exists(path):
        return None
    seq = []
    with open(path, newline="") as f:
        for r in csv.DictReader(f):
            rid = (r.get("numberOneRecordId") or "").strip().strip('"')
            if rid:
                seq.append(rid)
    # collapse into spells
    spells = []
    for rid in seq:
        if not spells or spells[-1][0] != rid:
            spells.append([rid, 1])
        else:
            spells[-1][1] += 1
    seen = set()
    returns = 0
    for rid, _ in spells:
        if rid in seen:
            returns += 1
        seen.add(rid)
    total = len(spells)
    uniq = len(seen)
    return dict(run=run, weeks=len(seq), spells=total, unique=uniq,
                returns=returns, pct=100.0*returns/total if total else 0.0)

if __name__ == "__main__":
    runs = sys.argv[1:] or ["radio-vitality-ret-1001"]
    print(f"{'run':40s} {'wk':>4s} {'spells':>6s} {'uniq':>5s} {'ret':>4s} {'ret%':>6s}")
    for run in runs:
        d = analyze(run)
        if d is None:
            print(f"{run:40s}  (no weeks.csv)")
            continue
        flag = "  <-- OVER 14%" if d["pct"] > 14.0 else ""
        print(f"{d['run']:40s} {d['weeks']:4d} {d['spells']:6d} {d['unique']:5d} "
              f"{d['returns']:4d} {d['pct']:6.1f}{flag}")
