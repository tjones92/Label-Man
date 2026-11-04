"""Compare a completed capital-risk probe with its archived cadence/capacity control.

python SimTools/analyze_single_capital_risk.py --control erc-v2-w17 --treated scr-v2 --seeds 1001
Reports attempt and unique-slot counts separately; repeats are not independent bets.
Legacy outcomes exclude historical promotion: the probe explicitly uses the current campaign as a proxy.
"""
import argparse
import csv
import itertools
import json
import math
from pathlib import Path

import pandas as pd

from analyze_studio_parent import BIG5, studio_ids

ROOT = Path(__file__).resolve().parents[1] / 'SimLogs'


def manifest(tag):
    return json.loads((ROOT / tag / 'runs.json').read_text(encoding='utf-8-sig'))


def prefix(tag, seed, weeks):
    return ROOT / f'{tag}-default-{seed}-{weeks}'


def read(p, stream, **kw):
    return pd.read_csv(f'{p}-{stream}.csv', **kw)


def replay(old, new, through=12):
    checked = []
    for path in sorted(ROOT.glob(old.name + '-*.csv')):
        suffix = path.name[len(old.name):]
        with path.open(encoding='utf-8-sig', newline='') as f:
            header = next(csv.reader(f))
        if 'week' not in header:
            continue
        def rows(file):
            with file.open(encoding='utf-8-sig', newline='') as f:
                reader = csv.DictReader(f)
                if reader.fieldnames != header:
                    raise ValueError(f'Schema changed: {suffix}')
                for row in reader:
                    if row['week'] and 1 <= int(row['week']) <= through:
                        yield row
        for n, (a, b) in enumerate(itertools.zip_longest(rows(path), rows(Path(str(new) + suffix))), 1):
            if a != b:
                raise ValueError(f'Switch-off mismatch: {suffix} row {n}')
        checked.append(suffix)
    if len(checked) < 50:
        raise ValueError('Incomplete switch-off replay')
    return dict(throughWeek=through, identicalStreams=checked)


def annual(p, through):
    launches = read(p, 'single-launch-quality')
    finance = read(p, 'label-finance')
    records = read(p, 'records', usecols=['week', 'year', 'recordId', 'artistId', 'labelId', 'currentPosition'])
    sales = read(p, 'label-sales-weekly')
    clearing = read(p, 'market-clearing-weekly')
    observed = clearing[clearing.week <= through]
    if observed.reconciliationDelta.abs().max() > 1 or observed.settlementDelta.abs().max() > 1:
        raise ValueError('Market settlement did not reconcile')
    chart = records[(records.week <= through) & records.currentPosition.between(1, 100)]
    if len(chart.groupby('week')) != through or not chart.groupby('week').size().eq(100).all():
        raise ValueError('Incomplete Hot 100 observation window')
    studios = studio_ids(lambda s: f'{p}-{s}.csv')
    result = []
    for year in range(1960, 1960 + through // 52):
        end = (year - 1959) * 52
        q = launches[(launches.year == year) & (launches.week <= end)]
        r = records[(records.year == year) & (records.week <= end)]
        s = sales[(sales.year == year) & (sales.week <= end)]
        c = clearing[(clearing.year == year) & (clearing.week <= end)]
        singles = s[s['format'] == 'Single']
        top = r[r.currentPosition.between(1, 10)]
        fin = finance[finance.week <= end].drop_duplicates('labelId', keep='last')
        row = dict(year=year, singles=len(q), big5=int(q.labelId.isin(BIG5).sum()),
                   studio=int(q.labelId.isin(studios).sum()), singleUnitsM=singles.units.sum() / 1e6,
                   lpUnitsM=s.loc[s['format'] == 'Album', 'units'].sum() / 1e6,
                   top10UnitsM=singles.top10Units.sum() / 1e6,
                   big5Top10Share=top.labelId.isin(BIG5).mean(), studioTop10Share=top.labelId.isin(studios).mean(),
                   hot100Acts=r.loc[r.currentPosition.between(1, 100), 'artistId'].nunique(), top10Acts=top.artistId.nunique(),
                   closed=fin.status.isin(['Bankrupt', 'Defunct']).sum(), baseCapacity=c.baseCapacity.sum(),
                   sellThrough=c.clearedSingleUnits.sum() / c.serviceableSingleIntent.sum())
        for tier in ('Major', 'MidTier', 'Independent', 'Small', 'Boutique'):
            row[tier] = int(q.tier.eq(tier).sum())
        result.append(row)
    return result


def diagnose(p, through):
    risk = read(p, 'single-capital-risk')
    risk = risk[risk.week <= through]
    if risk.empty or (~risk.commit & ((risk.samples == 0) | (risk.forecastNet <= 0))).any():
        raise ValueError('Risk gate refused an unevidenced or nonpositive-mean bet')
    numbers = risk.select_dtypes(include='number')
    if not numbers.map(math.isfinite).all().all() or (risk.capital < 0).any() or (risk.riskPremium < 0).any():
        raise ValueError('Invalid risk arithmetic')
    if not risk.lossProbability.between(0, 1).all() or not risk.exhaustionProbability.between(0, 1).all():
        raise ValueError('Invalid risk probabilities')
    # A successful reservation is not an issued record; retain the slate's actual terminal state.
    slate = read(p, 'release-slate')
    held = slate[(slate.reason == 'single-capital-risk') & (slate.week <= through)]
    if held.paidProduction.ne(0).any():
        raise ValueError('Risk refusal happened after recording was paid')
    issued = set(slate.loc[(slate.state == 'Issued') & (slate.week <= through), 'slotId'])
    rows = []
    for tier, q in risk.groupby('tier'):
        evidence = q[q.samples > 0]
        last = q.sort_values('week').drop_duplicates('labelId', keep='last')
        no = q[~q.commit]
        denied = set(no.slotId)
        positive_mean = no[no.forecastNet > 0]
        rows.append(dict(tier=tier, attempts=len(q), slots=q.slotId.nunique(), assessedLabels=len(last),
            lastMedianSamplesPerLabel=last.samples.median(), lastNoEvidenceLabels=int(last.samples.eq(0).sum()), noEvidenceAttempts=int(q.samples.eq(0).sum()),
            refusedAttempts=len(no), refusedSlots=len(denied), refusedPositiveMeanSlots=positive_mean.slotId.nunique(),
            zeroCapitalRefusedAttempts=int(no.capital.le(0).sum()), positiveCapitalRefusedSlots=no.loc[no.capital > 0, 'slotId'].nunique(),
            noObservedLossRefusedSlots=no.loc[(no.capital > 0) & (no.lossProbability == 0), 'slotId'].nunique(),
            dispersionOnlyRefusedSlots=no.loc[(no.capital > 0) & (no.exhaustionProbability == 0), 'slotId'].nunique(),
            exhaustionRefusedSlots=no.loc[no.exhaustionProbability > 0, 'slotId'].nunique(),
            refusedSlotsEventuallyIssued=len(denied & issued), medianSamples=evidence.samples.median(),
            medianLossProbability=evidence.lossProbability.median(), medianCapitalInOverheadMonths=(evidence.capital / evidence.overhead).median(),
            medianPremium=evidence.riskPremium.median(), medianForecast=evidence.forecastNet.median(),
            medianPremiumOverForecast=(evidence.riskPremium / evidence.forecastNet.where(evidence.forecastNet > 0)).median()))
    return rows, diagnose_outcomes(p, through)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--control', default='erc-v2-w17')
    ap.add_argument('--treated', default='scr-v2')
    ap.add_argument('--seeds', nargs='+', type=int, default=[1001])
    args = ap.parse_args()
    manifests = {a: manifest(t) for a, t in [('control', args.control), ('treated', args.treated)]}
    control, treated = manifests.values()
    doses = [f for f in treated['extraFlags'] if f.startswith('--single-capital-risk=')]
    if len(doses) != 1 or [f for f in treated['extraFlags'] if f not in doses] != control['extraFlags']:
        raise ValueError('Bundle differs beyond the risk probe')
    through = ((min(control['weeks'], treated['weeks']) - 1) // 52) * 52
    if through < 52:
        raise ValueError('Need at least one complete annual observation window')
    output = ROOT / args.treated / 'review'
    output.mkdir(parents=True, exist_ok=True)
    annual_rows, risk_rows, loss_rows = [], [], []
    for seed in args.seeds:
        capacity = []
        for arm, tag in [('control', args.control), ('treated', args.treated)]:
            m = manifests[arm]
            runs = [r for r in m['results'] if r['seed'] == seed and r['passed'] and r['exitCode'] == 0]
            if len(runs) != 1 or m['profile'] != 'game':
                raise ValueError(f'Incomplete run {arm}, {seed}')
            p = prefix(tag, seed, m['weeks'])
            c = read(p, 'market-clearing-weekly', usecols=['week', 'regionId', 'baseCapacity', 'albumChannelCapacity'])
            capacity.append(c[c.week <= through].sort_values(['week', 'regionId']).reset_index(drop=True))
            annual_rows.extend(dict(arm=arm, seed=seed, **r) for r in annual(p, through))
            if arm == 'treated':
                risk, losses = diagnose(p, through)
                risk_rows.extend(dict(seed=seed, **r) for r in risk)
            else:
                losses = diagnose_outcomes(p, through)
            loss_rows.extend(dict(arm=arm, seed=seed, **r) for r in losses)
        pd.testing.assert_frame_equal(*capacity)
    a = pd.DataFrame(annual_rows)
    if a.groupby(['seed', 'year']).baseCapacity.nunique().gt(1).any():
        raise ValueError('Authored purchase capacity changed')
    for name, frame in [('annual', a), ('risk', pd.DataFrame(risk_rows)), ('losses', pd.DataFrame(loss_rows))]:
        frame.to_csv(output / f'{name}.csv', index=False)
        print(name + '\n' + frame.round(4).to_string(index=False))
    identity = replay(prefix(args.control, args.seeds[0], control['weeks']), prefix(args.treated + '-off13', args.seeds[0], 13))
    (output / 'validation.json').write_text(json.dumps(dict(seeds=args.seeds, throughWeek=through, dose=doses[0], replay=identity), indent=2))
    print(f'Switch off: {len(identity["identicalStreams"])} identical week-keyed streams through week 12.')


def diagnose_outcomes(p, through):
    launches = read(p, 'single-launch-quality')
    mature = launches[launches.week <= through - 26].set_index('singleId').tier
    outcomes = read(p, 'release-outcomes')
    outcomes = outcomes[(outcomes.week <= through) & outcomes.recordId.isin(mature.index)].drop_duplicates('recordId')
    outcomes['tier'] = outcomes.recordId.map(mature)
    return [dict(tier=t, retired=len(q), lossShare=q.realizedNet.lt(0).mean(), medianNet=q.realizedNet.median(), meanNet=q.realizedNet.mean())
            for t, q in outcomes.groupby('tier')]


if __name__ == '__main__':
    main()
