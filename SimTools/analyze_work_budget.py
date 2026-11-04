"""Per-label work budget screen (`--slate-work-budget=S`), paired by seed against a control read through week 52.

Usage: python SimTools/analyze_work_budget.py --control tpk-v2-t90:265 --arm s067=wbg-v1-s067:53 --arm s100=wbg-v1-s100:53
Each spec is TAG:WEEKS; files are SimLogs/TAG-default-SEED-WEEKS-*.csv. Opening firms are the labels with a week-1
finance row, by initial tier. r is cleared over serviceable single intent (market-clearing-weekly.csv), the singles
purse's rationing factor. The Major off-chart base is .12 x settled live single units, the share the bundle runs at.
Loss share counts singles issued in weeks 1-26 and retired by week 52, so a five-year control reads the same window.
"""
import argparse, math, os
import pandas as pd

ROOT = os.path.join(os.path.dirname(__file__), '..', 'SimLogs')
TIERS = ('MidTier', 'Independent', 'Small', 'Boutique')
REASONS = ('preparation-capacity-unavailable', 'issue-capacity-unavailable', 'insufficient-unreserved-cash',
           'promotion-congestion', 'preparation-material-economics-or-funding', 'material-economics-funding-or-availability')
BASE_SHARE = .12


def read(tag, seed, weeks, name, **kw):
    return pd.read_csv(os.path.join(ROOT, f'{tag}-default-{seed}-{weeks}-{name}.csv'), **kw)


def measure(tag, seed, weeks):
    m = {}
    fin = read(tag, seed, weeks, 'label-finance', usecols=['week', 'labelId', 'labelTier', 'status', 'exitReason', 'cashReserves'])
    fin = fin[fin.week <= 52]
    opening = fin[fin.week == 1].set_index('labelId').labelTier
    exits = fin[~fin.exitReason.fillna('None').isin(['', 'None'])].groupby('labelId').week.min()
    q = read(tag, seed, weeks, 'single-launch-quality', usecols=['week', 'singleId', 'labelId', 'tier'])
    q = q[q.week <= 52]
    census = read(tag, seed, weeks, 'demand-release-census', usecols=['recordId', 'labelId', 'format', 'observationKind', 'observedAuditWeek'])
    lps = census[(census.format == 'Album') & (census.observationKind != 'Opening') & (census.observedAuditWeek <= 52)]
    slate = read(tag, seed, weeks, 'release-slate', usecols=['week', 'labelId', 'format', 'state', 'reason'])
    slate = slate[(slate.week <= 26) & (slate.state == 'Postponed')]
    for tier in TIERS:
        ids = set(opening[opening == tier].index)
        n = max(1, len(ids))
        mine = q[q.labelId.isin(ids)]
        m[f'{tier} labels'] = len(ids)
        for lo, hi in ((1, 13), (14, 26), (27, 52)):
            m[f'{tier} singles/label w{lo}-{hi}'] = ((mine.week >= lo) & (mine.week <= hi)).sum() / n
        m[f'{tier} LPs/label w1-52'] = lps.labelId.isin(ids).sum() / n
        m[f'{tier} closed by w52'] = int(sum(1 for i in ids if i in exits.index))
        if tier == 'MidTier':
            for week in (13, 26):
                at = fin[(fin.week == week) & fin.labelId.isin(ids)]
                m[f'MidTier Struggling at w{week}'] = int((at.status == 'Struggling').sum())
            for week in (26, 52):
                live = fin[(fin.week == week) & fin.labelId.isin(ids) & ~fin.labelId.isin(exits.index)]
                m[f'MidTier median cash at w{week}, firms surviving w52 ($k)'] = live.cashReserves.median() / 1e3
            mid = slate[slate.labelId.isin(ids)]
            for reason in REASONS:
                m[f'MidTier postponed w1-26: {reason}'] = (mid.reason == reason).sum() / n
    m['singles issued w1-52'] = len(q)
    for tier, group in q.groupby('tier'): m[f'singles issued w1-52: {tier}'] = len(group)
    clear = read(tag, seed, weeks, 'market-clearing-weekly', usecols=['week', 'serviceableSingleIntent', 'clearedSingleUnits', 'baseCapacity'])
    clear = clear[clear.week <= 52]
    m['r (cleared / serviceable singles)'] = clear.clearedSingleUnits.sum() / max(1, clear.serviceableSingleIntent.sum())
    m['serviceable single intent (M)'] = clear.serviceableSingleIntent.sum() / 1e6
    m['cleared single units (M)'] = clear.clearedSingleUnits.sum() / 1e6
    sales = read(tag, seed, weeks, 'label-sales-weekly')
    sales = sales[(sales.week <= 52) & (sales.format == 'Single')]
    units = sales.units.sum()
    m['chart-week unit share'] = sales.chartedUnits.sum() / max(1, units)
    m['Top 10 units (M)'] = sales.top10Units.sum() / 1e6
    tier_of = q.drop_duplicates('labelId').set_index('labelId').tier
    major_units = sales[sales.labelId.map(tier_of) == 'Major'].units.sum()
    m['Major unit share (base incl.)'] = (major_units + BASE_SHARE * units) / ((1 + BASE_SHARE) * units)
    rec = read(tag, seed, weeks, 'records', usecols=['week', 'recordId', 'currentPosition'])
    rec = rec[(rec.week <= 52) & (rec.currentPosition > 0)]
    m['Top 10 records'] = rec[rec.currentPosition <= 10].recordId.nunique()
    m['Hot 100 records'] = rec.recordId.nunique()
    out = read(tag, seed, weeks, 'release-outcomes', usecols=['week', 'recordId', 'format', 'realizedNet'])
    early = q[q.week <= 26].set_index('singleId').tier
    out = out[(out.format == 'Single') & (out.week <= 52) & out.recordId.isin(early.index)]
    out['tier'] = out.recordId.map(early)
    for tier in ('Major',) + TIERS:
        t = out[out.tier == tier]
        m[f'loss share, retired w1-26 singles: {tier}'] = (t.realizedNet < 0).mean() if len(t) else float('nan')
    return m


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--control', required=True)
    p.add_argument('--arm', action='append', required=True, help='NAME=TAG:WEEKS')
    p.add_argument('--seeds', nargs='+', type=int, default=[1001, 2002, 3003])
    p.add_argument('--output', help='CSV path for the paired table')
    a = p.parse_args()
    ctag, cweeks = a.control.split(':')
    control = {s: measure(ctag, s, int(cweeks)) for s in a.seeds}
    rows = []
    keys = list(control[a.seeds[0]])
    arms = []
    for spec in a.arm:
        name, rest = spec.split('=')
        tag, weeks = rest.split(':')
        arms.append((name, {s: measure(tag, s, int(weeks)) for s in a.seeds}))
    for k in keys:
        row = {'metric': k, 'control': sum(control[s][k] for s in a.seeds) / len(a.seeds)}
        for name, data in arms:
            d = [data[s].get(k, 0) - control[s][k] for s in a.seeds]
            mean = sum(d) / len(d)
            se = math.sqrt(sum((x - mean) ** 2 for x in d) / (len(d) - 1) / len(d)) if len(d) > 1 else float('nan')
            row[name] = sum(data[s].get(k, 0) for s in a.seeds) / len(a.seeds)
            row[f'{name} delta'] = mean
            row[f'{name} se'] = se
            row[f'{name} sign'] = f"{sum(x > 0 for x in d) if mean >= 0 else sum(x < 0 for x in d)}/{len(d)}"
        rows.append(row)
    frame = pd.DataFrame(rows)
    pd.set_option('display.width', 250); pd.set_option('display.max_rows', 200)
    print(frame.to_string(index=False, float_format=lambda x: f'{x:.3f}'))
    if a.output: frame.to_csv(a.output, index=False)


if __name__ == '__main__':
    main()
