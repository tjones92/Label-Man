"""Matched hit-act screen; studio-parent-labels must be absent from every manifest.

Annual repeat counts include carryovers. Debuts are first observed entries in this
run, excluding opening catalog. Cohorts use prior calendar year's Top 10 acts,
never future success. Group is the selling label at the act's first Top 10 that
year (Big-5, named studio labels, field); movers are assigned once.
"""
import argparse
import json
import re
import statistics
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import ROOT, path, screen
from analyze_studio_parent import BIG5, STUDIO_NAMES


def repeats(records):
    counts = records.groupby('artistId').recordId.nunique()
    return {'acts': len(counts), 'repeat2': int((counts >= 2).sum()),
            'repeat3': int((counts >= 3).sum())}


def departure_followup(records, events, opening, end_week):
    """Deduplicate departure streams and exclude incompletely followed hit acts."""
    hits = records[records.currentPosition.between(1, 10) & ~records.recordId.isin(opening)]
    hits = hits.sort_values('week').drop_duplicates('artistId')
    hits = hits[hits.week <= end_week - 52]
    departures = events[events.eventType.isin(['drop', 'performance-departure', 'performance-exhaustion'])]
    left = sum(bool(((departures.artistId == h.artistId) & departures.week.between(h.week, h.week + 52)).any())
               for h in hits.itertuples())
    return {'hitActFollowupCohort': len(hits), 'hitActsLeavingWithin52Weeks': left,
            'hitAct52WeekDepartureShare': left / len(hits) if len(hits) else None}


def annual_metrics(records, launches, opening, years=range(1960, 1965)):
    """Pure chart/cohort calculation, also used by fixture tests."""
    records = records.sort_values(['week', 'currentPosition'])
    chart = records[records.currentPosition.between(1, 100)]
    tops = chart[chart.currentPosition <= 10]
    entries = {n: chart[chart.currentPosition <= n].drop_duplicates('recordId')
               for n in (10, 40, 100)}
    entries = {n: e[~e.recordId.isin(opening)] for n, e in entries.items()}
    out = {}
    for year in years:
        c, t = chart[chart.year == year], tops[tops.year == year]
        if c.empty:
            continue
        row = repeats(t)
        row['hot100Acts'] = int(c.artistId.nunique())
        row['hot100Records'] = int(c.recordId.nunique())
        row['top10Records'] = int(t.recordId.nunique())
        row['top10WeeksPerRecord'] = len(t) / max(1, t.recordId.nunique())
        nth = t.groupby('recordId').agg(artistId=('artistId', 'first'), first=('week', 'min'), weeks=('week', 'size'))
        nth = nth.sort_values(['first', 'recordId'])
        nth['n'] = nth.groupby('artistId').cumcount() + 1
        for name, mask in [('first', nth.n == 1), ('second', nth.n == 2), ('thirdPlus', nth.n >= 3)]:
            row[f'top10Weeks_{name}'] = float(nth.loc[mask, 'weeks'].mean()) if mask.any() else None
        row['labelsWithTop10'] = int(t.labelId.nunique())
        row['top10SlotHHI'] = float((t.labelId.value_counts(normalize=True) ** 2).sum())
        for n in (1, 3, 5, 10):
            row[f'top{n}LabelSlotShare'] = float(t.labelId.value_counts(normalize=True).head(n).sum())
        row['numberOneRecords'] = int(t[t.currentPosition == 1].recordId.nunique())
        ones = t[t.currentPosition == 1].sort_values('week')
        if len(ones):
            runs = (ones.recordId.ne(ones.recordId.shift()) | ones.week.diff().ne(1)).cumsum()
            row['maxNumberOneStreak'] = int(ones.groupby(runs).size().max())
        # Every repeat act belongs to exactly one group, even if it changes labels.
        assignments = t.drop_duplicates('artistId').set_index('artistId')['group']
        for group in ('big5', 'studio', 'field'):
            acts = assignments[assignments == group].index
            row.update({f'{group}_{k}': v for k, v in repeats(t[t.artistId.isin(acts)]).items()})
            row[f'{group}_top10Share'] = float((t['group'] == group).mean())
        cohort = set(tops.loc[tops.year == year - 1, 'artistId'])
        if cohort:
            q = launches[(launches.year == year) & launches.artistId.isin(cohort)]
            hot = c[c.artistId.isin(cohort)]
            concurrency = hot.groupby(['week', 'artistId']).recordId.nunique()
            row['priorHitActs'] = len(cohort)
            row['singlesPerPriorHitAct'] = len(q) / len(cohort)
            row['silentPriorHitActs'] = len(cohort - set(q.artistId))
            gaps = q.loc[q.weeksSinceLastSingle >= 0, 'weeksSinceLastSingle']
            row['medianLaunchGap'] = float(gaps.median()) if len(gaps) else None
            row['concurrentChartWeekShare'] = float((concurrency >= 2).mean()) if len(concurrency) else None
            for n, e in entries.items():
                e = e[(e.year == year) & e.artistId.isin(cohort)]
                row[f'top{n}DebutsPerPriorHitAct'] = len(e) / len(cohort)
            # Conversion of this year's Hot 100 debuts, followed to the end of the run.
            e = entries[100]
            ids = set(e.loc[(e.year == year) & e.artistId.isin(cohort), 'recordId'])
            row['priorHitDebutToTop10'] = len(ids & set(entries[10].recordId)) / len(ids) if ids else None
        out[year] = row
    return out


def measure(tag, seed, weeks):
    p = lambda stream: path(tag, seed, stream, weeks)
    manifest = json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig'))
    if '--studio-parent-labels' in manifest['extraFlags']:
        raise ValueError('Studio switch must be off for the cadence screen')
    completed = [r for r in manifest['results'] if r['seed'] == seed]
    if len(completed) != 1 or not completed[0]['passed'] or manifest['weeks'] != weeks:
        raise ValueError('Missing or incomplete native audit')
    records = pd.read_csv(p('records'), usecols=['week', 'year', 'artistId', 'recordId', 'labelId', 'currentPosition'])
    if records.duplicated(['week', 'recordId']).any():
        raise ValueError('Duplicate chart record/week')
    directory = pd.read_csv(p('label-directory'))
    studios = set(directory.loc[directory.labelName.isin(STUDIO_NAMES), 'labelId'])
    records['group'] = records.labelId.map(lambda i: 'big5' if i in BIG5 else 'studio' if i in studios else 'field')
    q = pd.read_csv(p('single-launch-quality'))
    if q.singleId.duplicated().any():
        raise ValueError('Duplicate single launch')
    census = pd.read_csv(p('demand-release-census'))
    opening = set(census.loc[census.observationKind == 'Opening', 'recordId'])
    yearly = annual_metrics(records, q, opening)
    guards = screen(tag, seed, weeks)  # includes exact weekly sales/clearing reconciliation
    guards['singleUnitsInclOffM'] = guards['liveSingles(M)'] + guards['offChart(M)']
    strategy = pd.read_csv(p('release-strategy'))
    guards['strategyDecisions'] = len(strategy)
    guards['singleLaunches'] = len(q)
    guards['albumLaunches'] = int(((census.observationKind == 'Launch') & (census.format == 'Album')).sum())
    events = pd.read_csv(p('artist-population-events'))
    for event in ('drop', 'performance-departure', 're-signing'):
        guards[f'events_{event}'] = int((events.eventType == event).sum())
    for reason in ('Performance', 'ContractExpired', 'LabelClosure'):
        guards[f'drops_{reason}'] = int(((events.eventType == 'drop') & (events.dropReason == reason)).sum())
    # A complete 52-week follow-up of each act's first observed non-opening Top 10.
    # Departure events can be duplicated across the drop and career-ladder streams;
    # count each act once, not each event, and do not treat censoring as retention.
    guards.update(departure_followup(records, events, opening, weeks))
    # Read the adopted benchmark directly; avoid an analysis-only copy drifting from the model.
    source = (Path(ROOT).parent / 'Data' / 'GenreCatalog.cs').read_text(encoding='utf-8-sig')
    block = source.split('ChartSlotBenchmark = new()', 1)[1].split('};', 1)[0]
    benchmark = {g: [int(v) for v in re.findall(r'\d+', values)]
                 for g, values in re.findall(r'\[Genre\.(\w+)\]\s*=\s*new\[\]\s*\{([^}]+)\}', block)}
    if not benchmark:
        raise ValueError('Could not read GenreCatalog.ChartSlotBenchmark')
    year_end = pd.read_csv(p('year-end-hot100'))
    for year in range(1960, 1965):
        slots = year_end[year_end.year == year].groupby('genre').yearEndSlots.sum()
        guards[f'genreAbsoluteSlotError_{year}'] = sum(abs(slots.get(g, 0) - values[year - 1960])
                                                     for g, values in benchmark.items())
        for genre, count in slots.items():
            guards[f'yearEnd_{year}_{genre}'] = int(count)
    ay = pd.read_csv(p('act-release-years'))
    hot = ay[(ay.top10AtStart > 0) & ay.year.between(1961, 1964)]
    guards['laggedTop10SinglesPer52SignedWeeks'] = 52 * hot.singles.sum() / max(1, hot.signedWeeks.sum())
    guards['laggedTop10LpsScheduledPer52SignedWeeks'] = 52 * hot.lpsScheduled.sum() / max(1, hot.signedWeeks.sum())
    log = (Path(ROOT) / tag / f'{tag}-default-{seed}-{weeks}-console.log').read_text(encoding='utf-8', errors='replace')
    telemetry = [line for line in log.splitlines() if line.startswith('HIT_ACT_CADENCE ')]
    return {'annual': yearly, 'guards': guards, 'telemetry': telemetry}


def identity(tag, seed, control, weeks):
    prefix = f'{tag}-default-{seed}-{weeks}-'
    files = sorted(Path(ROOT).glob(prefix + '*.csv'))
    reference = sorted(Path(ROOT).glob(f'{control}-default-{seed}-{weeks}-*.csv'))
    names = {f.name[len(prefix):] for f in files}
    expected = {f.name[len(f'{control}-default-{seed}-{weeks}-'):] for f in reference}
    if not names or names != expected:
        raise ValueError('Identity replay stream sets differ or are empty')
    different = [name for name in names if Path(path(tag, seed, name[:-4], weeks)).read_bytes()
                 != Path(path(control, seed, name[:-4], weeks)).read_bytes()]
    if different:
        raise ValueError(f'Off-switch replay differs: {different}')
    return {'streams': len(names), 'allByteIdentical': True, 'seed': seed}


def history():
    out = {}
    for year in range(1960, 1965):
        h = pd.read_csv(Path(ROOT) / f'historical-top10-{year}' / f'records_{year}_grouped.csv')
        n = h.groupby('artist').title.nunique()
        out[year] = {'repeat2': int((n >= 2).sum()), 'repeat3': int((n >= 3).sum()),
                     'top10Records': len(h), 'top10WeeksPerRecord': float(h.slots.sum() / len(h))}
    return out


def write_readout(frame, result, output):
    means = frame.groupby(level='arm').mean()
    arms = ['control', 'lane', 'retention', 'both']
    lines = ['# Hit-act cadence: matched screen', '',
             'Studio switch off throughout; control `ttx-v1-tax`. Three matched seeds, 265 weeks.', '',
             f"Off replay: **{result['identity']['streams']} streams byte-identical**.", '',
             '| Metric (three-seed mean) | Control | Lane + split | Retention | Both |',
             '|---|---:|---:|---:|---:|']
    metrics = [('1960 acts with 2+ Top 10 records', '1960_repeat2', False),
               ('1960 acts with 3+ Top 10 records', '1960_repeat3', False),
               ('1960–64 acts with 2+, per year', 'meanRepeat2', False),
               ('1960–64 acts with 3+, per year', 'meanRepeat3', False),
               ('1960–64 Top 10 weeks/record, mean of annual means', 'meanTop10Tenure', False),
               ('Hit-act departure within 52 weeks', 'hitAct52WeekDepartureShare', True),
               ('Singles/52 signed weeks, lagged lifetime Top 10 acts', 'laggedTop10SinglesPer52SignedWeeks', False),
               ('LPs scheduled/52 signed weeks, same cohort', 'laggedTop10LpsScheduledPer52SignedWeeks', False),
               ('Recorded strategy decisions', 'strategyDecisions', False), ('Single launches', 'singleLaunches', False),
               ('Actual album launches', 'albumLaunches', False),
               ('Single units incl. off-chart (millions)', 'singleUnitsInclOffM', False),
               ('Major single-unit share incl. off-chart', 'majorShareInclOff', True),
               ('Big-5 Top 10 slot share, 1961', '1961_big5_top10Share', True),
               ('Small operating closures', 'closed_Small', True),
               ('Boutique operating closures', 'closed_Boutique', True),
               ('Independent operating closures', 'closed_Independent', True)]
    for label, key, percent in metrics:
        cells = [f'{means.loc[a, key] * 100:.2f}%' if percent else f'{means.loc[a, key]:.2f}' for a in arms]
        lines.append('| ' + label + ' | ' + ' | '.join(cells) + ' |')
    lines += ['', 'Annual repeat counts below are **2+ / 3+** distinct Top 10 records, including carryovers.', '',
              '| Year | History | Control | Lane + split | Retention | Both |', '|---|---:|---:|---:|---:|---:|']
    for year, h in result['history'].items():
        cells = [f"{h['repeat2']} / {h['repeat3']}"]
        cells += [f"{means.loc[a, f'{year}_repeat2']:.1f} / {means.loc[a, f'{year}_repeat3']:.1f}" for a in arms]
        lines.append(f'| {year} | ' + ' | '.join(cells) + ' |')
    lines += ['', 'Per-seed values and all concentration, genre, tenure and cohort diagnostics: `screen.csv`.',
              'Paired differences with seed SEs and native mechanism ledgers: `review.json`.', '',
              'Annual chart outcomes cover 1960–64; economy guards cover the full matched 265 weeks.',
              'Compact release-capacity/seasonality streams are header-only; strategy rows do not measure fired release rolls.',
              'Three seeds understate reshuffle uncertainty. A better release gap alone is not an adoption decision.',
              'The studio probe must be re-screened after an accepted cadence base lands.', '']
    (output / 'screen.md').write_text('\n'.join(lines), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--control', default='ttx-v1-tax')
    parser.add_argument('--tag', default='hac-v2')
    parser.add_argument('--seeds', nargs='+', type=int, default=[1001, 2002, 3003])
    parser.add_argument('--weeks', type=int, default=265)
    args = parser.parse_args()
    output = Path(ROOT) / args.tag
    control_flags = json.loads((Path(ROOT) / args.control / 'runs.json').read_text(encoding='utf-8-sig'))['extraFlags']
    extras = {'lane': ['--hit-act-cadence'], 'retention': ['--hit-act-retention'],
              'both': ['--hit-act-cadence', '--hit-act-retention']}
    for arm in ('lane', 'retention', 'both'):
        manifest = json.loads((Path(ROOT) / (args.tag + '-' + arm) / 'runs.json').read_text(encoding='utf-8-sig'))
        # Older cadence screens applied awareness sharing implicitly; new invocations name it explicitly.
        flags = [f for f in manifest['extraFlags'] if f != '--hit-act-awareness-split' or arm == 'retention']
        if sorted(flags) != sorted(control_flags + extras[arm]):
            raise ValueError(f'{arm} flags differ from the adopted control plus the intended treatment')
    result = {'definitions': __doc__, 'history': history(),
              'identity': identity(args.tag + '-off', args.seeds[0], args.control, args.weeks), 'runs': {}}
    flat = {}
    for arm, tag in [('control', args.control)] + [(a, args.tag + '-' + a) for a in ('lane', 'retention', 'both')]:
        for seed in args.seeds:
            run = measure(tag, seed, args.weeks)
            result['runs'][f'{arm}-{seed}'] = run
            flat[(arm, seed)] = dict(run['guards'])
            for year, metrics in run['annual'].items():
                flat[(arm, seed)].update({f'{year}_{k}': v for k, v in metrics.items()})
            for key, metric in [('meanRepeat2', 'repeat2'), ('meanRepeat3', 'repeat3'),
                                ('meanTop10Tenure', 'top10WeeksPerRecord')]:
                flat[(arm, seed)][key] = statistics.mean(v[metric] for v in run['annual'].values())
    frame = pd.DataFrame(flat).T
    frame.index.names = ['arm', 'seed']
    frame.to_csv(output / 'screen.csv')
    paired = {}
    for arm in ('lane', 'retention', 'both'):
        paired[arm] = {}
        for column in frame:
            diffs = [frame.loc[(arm, s), column] - frame.loc[('control', s), column] for s in args.seeds]
            if all(pd.notna(x) for x in diffs):
                paired[arm][column] = {'mean': statistics.mean(diffs),
                                     'se': statistics.stdev(diffs) / len(diffs) ** .5 if len(diffs) > 1 else None,
                                     'perSeed': diffs}
    result['paired'] = paired
    # Round-trip through pandas handles numpy scalars; JSON null represents missing observations.
    (output / 'review.json').write_text(pd.Series(result).to_json(indent=2), encoding='utf-8')
    write_readout(frame, result, output)
    print(frame.groupby(level='arm').mean().T.to_string(float_format=lambda x: f'{x:.4f}'))


if __name__ == '__main__':
    main()
