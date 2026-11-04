"""Validate shadow/active renewal reserve evidence and actual paid receipts.

--runs NAME=TAG[,TAG...] reads only completed native audit results. This is a
decision ledger; compare career/economic outcomes separately on matched worlds.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import ROOT


def read_lines(lines):
    decisions, receipts = [], []
    booleans = {'wants', 'original', 'eligible', 'proposed', 'applied'}
    numbers = {'cash', 'advance', 'fullReserve', 'goingReserve', 'taxService', 'nearTaxDue', 'cashFlow', 'audience', 'cashAfter'}
    for line in lines:
        if line.startswith('HIT_ACT_RENEWAL '):
            target = decisions
        elif line.startswith('HIT_ACT_RENEWAL_PAID '):
            target = receipts
        else:
            continue
        row = dict(token.split('=', 1) for token in line.split()[1:])
        for k, v in list(row.items()):
            if k in booleans:
                if v not in ['True', 'False']:
                    raise ValueError('Invalid renewal decision boolean')
                row[k] = v == 'True'
            elif k in numbers:
                row[k] = float(v)
            elif k in ['week', 'contractHits', 'sinceHit']:
                row[k] = int(v)
        target.append(row)
    return pd.DataFrame(decisions), pd.DataFrame(receipts)


def validate(decisions, receipts, active):
    if decisions.empty:
        raise ValueError('No renewal decisions observed')
    key = ['week', 'artist', 'label']
    if decisions.duplicated(key).any():
        raise ValueError('Duplicate renewal decision')
    eligible = decisions[decisions.eligible]
    if len(eligible) and ((eligible.tier != 'MidTier') | eligible.cashFlow.lt(0) |
                         eligible.contractHits.le(0) | ~eligible.sinceHit.between(0, 52)).any():
        raise ValueError('Renewal relaxation reached an ineligible contract')
    if (decisions.goingReserve - (decisions.fullReserve - decisions.taxService + decisions.nearTaxDue).clip(lower=0)).abs().gt(.01).any():
        raise ValueError('Operating/slate/tax reserve components do not reconcile')
    expected = decisions.wants & decisions.eligible & decisions.cash.sub(decisions.advance).gt(decisions.goingReserve)
    if decisions.proposed.ne(expected).any():
        raise ValueError('Proposed renewal ignored affordability or desire')
    applied = decisions[decisions.applied]
    if decisions.applied.ne(bool(active) & ~decisions.original & decisions.proposed).any():
        raise ValueError('Renewal applied outside the isolated gate')
    if not len(applied):
        if len(receipts):
            raise ValueError('Receipt without an applied renewal')
        return
    if receipts.empty or receipts.duplicated(key).any():
        raise ValueError('Missing/duplicate paid advance receipt')
    paid = applied.merge(receipts, on=key, how='outer', suffixes=('', 'Paid'), indicator=True, validate='one_to_one')
    if not paid['_merge'].eq('both').all():
        raise ValueError('Every new renewal must have exactly one paid receipt')
    tolerance = .1 + paid.cash.abs() * 1e-6
    if ((paid.cash - paid.cashAfter - paid.advancePaid).abs() > tolerance).any() or (paid.cashAfter + tolerance < paid.goingReserve).any():
        raise ValueError('Supported renewal failed to pay its advance or retain its reserve')


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--runs', nargs='+', required=True)
    ap.add_argument('--output', required=True)
    args = ap.parse_args()
    out = Path(args.output); out.mkdir(parents=True, exist_ok=True)
    all_d, all_p, rows = [], [], []
    for spec in args.runs:
        arm, tags = spec.split('=', 1)
        seen = set()
        for tag in tags.split(','):
            m = json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig'))
            active = '--hit-act-going-concern-renewal' in m['extraFlags']
            if not active and '--audit-hit-act-renewals' not in m['extraFlags']:
                raise ValueError('Run has no renewal audit')
            for r in m['results']:
                if not r['passed'] or r['seed'] in seen:
                    continue
                seen.add(r['seed'])
                log = Path(ROOT) / tag / f'{r["name"]}-console.log'
                d, p = read_lines(log.read_text(encoding='utf-8', errors='replace').splitlines())
                validate(d, p, active)
                all_d.append(d.assign(arm=arm, seed=r['seed']))
                if len(p): all_p.append(p.assign(arm=arm, seed=r['seed']))
                rows.append(dict(arm=arm, seed=r['seed'], reviews=len(d), desired=int(d.wants.sum()),
                    eligible=int(d.eligible.sum()), deniedOriginal=int((d.wants & ~d.original).sum()),
                    shadowWouldAllow=int((~d.original & d.proposed).sum()), applied=int(d.applied.sum()),
                    actualNewAdvances=float(p.advance.sum()) if len(p) else 0.))
    pd.concat(all_d).to_csv(out / 'renewal-decisions.csv', index=False)
    if all_p: pd.concat(all_p).to_csv(out / 'renewal-paid.csv', index=False)
    s = pd.DataFrame(rows); s.to_csv(out / 'renewal-summary.csv', index=False)
    lines = ['# Recent-hit renewal liquidity experiment', '',
        'Only completed runs. Shadow eligibility does not renew anyone; active grants must preserve the tested reserve and have a matching actual advance debit.', '',
        '| Arm | Seed | Reviews | Desired | Denied by original reserve | New gate would permit | Actually applied | Actual new advances |',
        '|---|---:|---:|---:|---:|---:|---:|---:|']
    for r in rows:
        lines.append(f'| {r["arm"]} | {r["seed"]} | {r["reviews"]} | {r["desired"]} | {r["deniedOriginal"]} | {r["shadowWouldAllow"]} | {r["applied"]} | ${r["actualNewAdvances"]:,.0f} |')
    lines += ['', 'No advance, tax payment, operating charge or slate cost is waived. The optional gate changes the reserve horizon for profitable recent-hit MidTier renewals only. Counterfactual contract outcomes and later chart careers diverge across worlds; this ledger isolates the immediate decision and payment, not the full causal decomposition.', '']
    (out / 'renewal-review.md').write_text('\n'.join(lines), encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
