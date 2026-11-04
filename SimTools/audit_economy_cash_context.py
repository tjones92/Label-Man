"""Read existing matched 1960-64 runs; write cash/turnover context without running a simulation.

python -I SimTools/audit_economy_cash_context.py
The ratios are diagnostics, not historical cash targets or full balance sheets.
"""
import csv
import json
import statistics as st
from collections import defaultdict
from pathlib import Path

TIERS = ('Small', 'Boutique', 'Independent', 'MidTier', 'Major')
INACTIVE = {'Bankrupt', 'Defunct', 'Acquired'}
def med(xs):
    return round(st.median(xs), 2) if xs else None

def audit(prefix):
    end, gross, contribution = {}, defaultdict(float), defaultdict(float)
    with open(prefix + '-label-finance.csv', encoding='utf-8-sig', newline='') as f:
        for r in csv.DictReader(f):
            week = int(r['week'])
            if week > 260:
                break
            if int(r['year']) == 1964:
                gross[r['labelId']] += float(r['weeklyGross'])
                contribution[r['labelId']] += float(r['weeklyNet'])
            if week == 260:
                end[r['labelId']] = r
    if not end:
        raise ValueError(f'{prefix}: missing week 260')
    active = [r for r in end.values() if r['status'] not in INACTIVE]
    out = {'prefix': prefix, 'totalCash': round(sum(float(r['cashReserves']) for r in active), 2), 'tiers': {}}
    for tier in TIERS:
        rs = [r for r in active if r['labelTier'] == tier]
        positive_gross = [r for r in rs if gross[r['labelId']] > 0]
        out['tiers'][tier] = {
            'n': len(rs),
            'cash': med([float(r['cashReserves']) for r in rs]),
            'cashLessPlantPayables': med([float(r['cashReserves']) - float(r['outstandingPlantPayables']) for r in rs]),
            'plantPayables': med([float(r['outstandingPlantPayables']) for r in rs]),
            'wholesaleReceivables': med([float(r['outstandingWholesaleReceivables']) for r in rs]),
            'gross1964': med([gross[r['labelId']] for r in rs]),
            'cashMonthsOf1964Gross': med([12 * float(r['cashReserves']) / gross[r['labelId']] for r in positive_gross]),
            'cashMonthsOfFixedOverhead': med([float(r['cashReserves']) / max(1, float(r['monthlyOverhead'])) for r in rs]),
            'positiveRecentCashFlowPct': round(100 * sum(float(r['recentMonthlyCashFlow']) > 0 for r in rs) / len(rs), 2),
            'cashLessPlantAboveAnnualGrossPct': round(100 * sum(float(r['cashReserves']) - float(r['outstandingPlantPayables']) > gross[r['labelId']] for r in positive_gross) / max(1, len(positive_gross)), 2),
            'historicalLabels': [{k: r[k] for k in ('labelName', 'cashReserves', 'outstandingPlantPayables', 'monthlyOverhead', 'lastMonthlyProfit')} | {'gross1964': round(gross[r['labelId']], 2)} for r in rs if r['isHistorical'].lower() == 'true']
        }
    return out

groups = {
    'original': 'SimLogs/decaboff-{}-522',
    'corrected': 'SimLogs/decabon-{}-522',
    'pressing': 'SimLogs/economy-retune-p2-larger-{}-260',
    'volume70Experimental': 'SimLogs/economy-wealth-p2-volume-{}-260',
}
result = {'horizon': 'week 260 / final chart week of 1964', 'definition': 'Cash is a stock; gross is 1964 booked record receipts. Cash minus plant bills is a partial liability adjustment, not unrestricted cash or net worth. Fixed-overhead coverage excludes variable release spending. Each reported mean below is the equal-weight mean of seed-level medians.', 'groups': {}}
for name, template in groups.items():
    runs = [audit(template.format(seed)) for seed in (1001, 2002)]
    result['groups'][name] = {'runs': runs, 'meanMediansByTier': {
        tier: {k: round(st.mean(r['tiers'][tier][k] for r in runs), 2)
               for k in runs[0]['tiers'][tier] if k not in ('n', 'historicalLabels')}
        for tier in TIERS}}
Path('SimLogs/economy-cash-historical-context.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
for name, group in result['groups'].items():
    print(name, json.dumps(group['meanMediansByTier']))
print('named pressing seed 2002', json.dumps({t: result['groups']['pressing']['runs'][1]['tiers'][t]['historicalLabels'] for t in ('MidTier', 'Major')}))
