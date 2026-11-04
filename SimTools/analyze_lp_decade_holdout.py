"""Audit the fixed LP calibration on two new, complete decade worlds."""
import argparse
import hashlib
import json
import os

import numpy as np
import pandas as pd

from analyze_budget_lp_products import analyse
from analyze_lp_rack_pool import require, settings
from analyze_hit_act_cadence import annual_metrics, history as hit_history
from analyze_head_hit_conversion import require_complete_chart
from analyze_rack_jobber_probe import BILLBOARD
from analyze_single_capital_risk import ROOT, read
from analyze_studio_parent import BIG5, studio_ids


ARM = 'budgetlp098pool102'
SEEDS = {27102701, 27102702}
YEARS = set(range(1960, 1970))


def review(tag, retained_seed=None):
    folder = ROOT / tag
    plan = json.loads((folder / 'plan.json').read_text(encoding='utf-8-sig'))
    require(plan['arms'] == [ARM] and set(plan['seeds']) == SEEDS and plan['weeks'] in (521, 530),
            'Require the prespecified candidate seeds and decade horizon')
    endpoint_names = []
    if retained_seed is None:
        require(len(plan['runs']) == 2 and {r['seed'] for r in plan['runs']} == SEEDS and
                all(r['passed'] and r['exitCode'] == 0 for r in plan['runs']),
                'Both decade worlds must finish successfully')
        runs = plan['runs']
    else:
        # User explicitly retained this seed after the overlong horizon caused the
        # peer to be terminated by runner cleanup. Never relabel the source run.
        require(retained_seed == 27102702 and plan['weeks'] == 530,
                'Only the documented retained endpoint world may use this exception')
        runs = [r for r in plan['runs'] if r['seed'] == retained_seed]
        require(len(runs) == 1, 'Missing retained endpoint world')
        endpoint_names = [runs[0]['name']]
    require(plan['checks'] and all(c['passed'] for c in plan['checks']), 'Source checks must pass')
    required_checks = {'native', 'budgetproducts', 'channelnative', 'channelroundtrip', 'accessroundtrip',
                       'lpcalibrationnative', 'lpcalibrationroundtrip', 'lpcalibrationstore'}
    require(required_checks <= {c['check'] for c in plan['checks']}, 'Missing native or save/load evidence')
    selected = json.loads((ROOT / 'lpdc-pool102-three-seed/plan.json').read_text(encoding='utf-8-sig'))
    require(plan['assemblySha256'] == selected['assemblySha256'], 'Candidate assembly changed')
    digest = hashlib.sha256((ROOT.parent / '.godot/mono/temp/bin/Debug/Label Man.dll').read_bytes()).hexdigest().upper()
    require(digest == plan['assemblySha256'], 'Current assembly differs from the audited assembly')
    expected_flags = settings(selected['runs'][0]) - {'--weeks=106'} | {f'--weeks={plan["weeks"]}'}
    for run in runs:
        require(settings(run) == expected_flags, 'Fixed candidate settings changed: ' + run['name'])
        require((ROOT / f"{run['name']}-lp-channel-choice-weekly.csv").exists(), 'Missing buyer-conservation stream')
    result = analyse(folder / 'plan.json', completed_only=bool(endpoint_names), endpoint_overrun_runs=endpoint_names)
    require(result['matrixComplete'] or endpoint_names, 'Incomplete holdout matrix')
    annual = pd.DataFrame(result['annual'])
    numerical = annual.select_dtypes(include='number')
    require(np.isfinite(numerical.drop(columns=['storeErrorM', 'rackErrorM'], errors='ignore')).all().all(),
            'Nonfinite decade telemetry')
    for seed, group in annual.groupby('seed'):
        full = group[group.year.isin(YEARS)]
        require(set(full.year) == YEARS and full.observedWeeks.isin([52, 53]).all() and not full.partial.any(),
                'Need all ten complete settlement years: ' + str(seed))
        require((full[['storesM', 'allRacksM', 'singlesM', 'meanLiveLps', 'budgetNet']] > 0).all().all(),
                'A retail, repertoire or budget-finance series collapses: ' + str(seed))
        prefix = ROOT / next(r['name'] for r in runs if r['seed'] == seed)
        front_finance = read(prefix, 'rack-finance-weekly')
        front = read(prefix, 'rack-region-weekly')
        require(front_finance.groupby('week').units.sum().sub(front.groupby('week').units.sum(), fill_value=0).abs().max() < .01,
                'Front-line financial sales do not reconcile')
        require(np.isfinite(front_finance.select_dtypes(include='number')).all().all(), 'Nonfinite front-line finance')
    annual['rawRackDemandM'] = annual.frontDemandM + annual.budgetDemandM
    annual['unfulfilledRackDemandM'] = annual.rawRackDemandM - annual.allRacksM
    annual['storesAndRacksM'] = annual.storesM + annual.allRacksM
    annual['storeErrorPct'] = annual.apply(lambda r: (r.storesM / BILLBOARD[r.year]['stores'] - 1) * 100
                                          if r.year in BILLBOARD and not r.partial else None, axis=1)
    annual['rackErrorPct'] = annual.apply(lambda r: (r.allRacksM / BILLBOARD[r.year]['racks'] - 1) * 100
                                         if r.year in BILLBOARD and not r.partial else None, axis=1)
    annual['meanChannelErrorPct'] = (annual.storeErrorPct.abs() + annual.rackErrorPct.abs()) / 2
    historical = annual[annual.year.isin(BILLBOARD) & ~annual.partial]
    # Observational return point for the user's separate repeat-hit question.
    # Reuse the existing identities/definitions without changing either run.
    hit_rows = []
    for run in runs:
        prefix = ROOT / run['name']
        chart = read(prefix, 'records', usecols=['week', 'year', 'artistId', 'recordId', 'labelId', 'currentPosition'])
        chart = chart[chart.currentPosition.between(1, 100)]
        observed_weeks = int(chart.week.max())
        require(observed_weeks == (522 if endpoint_names else plan['weeks']), 'Unexpected observed chart horizon')
        require_complete_chart(chart, observed_weeks)
        studios = studio_ids(lambda stream: f'{prefix}-{stream}.csv')
        chart['group'] = chart.labelId.map(lambda label: 'big5' if label in BIG5 else 'studio' if label in studios else 'field')
        launches = read(prefix, 'single-launch-quality')
        census = read(prefix, 'demand-release-census')
        opening = set(census.loc[census.observationKind == 'Opening', 'recordId'])
        hit_rows.extend(dict(seed=run['seed'], year=year, **row)
                        for year, row in annual_metrics(chart, launches, opening, years=sorted(YEARS)).items())
    hit_frame = pd.DataFrame(hit_rows)
    hit_frame.to_csv(folder / 'lp-decade-hit-acts.csv', index=False)
    result.update(annual=annual.astype(object).where(pd.notna(annual), None).to_dict('records'),
                  prespecifiedSeeds=sorted(SEEDS), completeYears=sorted(YEARS),
                  reviewedSeeds=sorted(annual.seed.unique().tolist()),
                  retainedSeedException=retained_seed,
                  historicalMeans=historical.groupby('year')[['storesM', 'allRacksM', 'storeErrorPct', 'rackErrorPct',
                                                               'meanChannelErrorPct']].mean().reset_index().to_dict('records'),
                  perSeedHistorical=historical.groupby('seed')[['storeErrorPct', 'rackErrorPct', 'meanChannelErrorPct']].mean().reset_index().to_dict('records'),
                  hitActAnnual=hit_frame.astype(object).where(pd.notna(hit_frame), None).to_dict('records'),
                  hitActHistorical1960to1964=hit_history(),
                  technicalChecksPassed=True, calibrationAccepted=False,
                  scope=('User retained seed 27102702 only. Its 522 observed chart weeks contain ten complete 1960-1969 settlement years '
                         'and one partial 1970 week. Native exit was 1 because the 530-week request exceeded the game end; '
                         'the original native failure and incomplete two-seed matrix remain explicit. Both interrupted peer runs are excluded. '
                         if endpoint_names else 'Fixed candidate on two previously unused seeds. Complete 1960-1969 years; partial years excluded. ') +
                        'Historical outlet references exist for 1961-1965 only. Financial/consumer identities are technical checks; '
                        'they do not by themselves establish a historical calibration. Promotion requires reviewing the reported channel errors and decade path.')
    (folder / 'lp-decade-review.json').write_text(json.dumps(result, indent=2) + '\n')
    annual.to_csv(folder / 'lp-decade-annual.csv', index=False)
    print(annual[['seed', 'year', 'observedWeeks', 'partial', 'storesM', 'allRacksM', 'singlesM',
                  'meanChannelErrorPct']].sort_values(['seed', 'year']).to_string(index=False))
    print('Historical means:', json.dumps(result['historicalMeans']))
    print('Per seed:', json.dumps(result['perSeedHistorical']))
    print('Max cash error:', max(c['cashMaxError'] for c in result['checks']))
    return annual


def plot(folder, annual):
    os.environ['MPLCONFIGDIR'] = str(folder / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 11})
    rows = annual[annual.year.isin(YEARS) & ~annual.partial]
    fig, axes = plt.subplots(2, 2, figsize=(13, 8.4))
    for seed, color, marker in [(27102701, '#2675a6', 'o'), (27102702, '#d77b18', 's')]:
        if seed not in set(rows.seed):
            continue
        group = rows[rows.seed == seed].sort_values('year')
        for ax, metric in zip(axes.flat, ['storesM', 'allRacksM', 'singlesM', 'meanLiveLps']):
            ax.plot(group.year, group[metric], color=color, marker=marker, label=f'Seed {seed}')
    years = sorted(BILLBOARD)
    for ax, key in zip(axes[0], ['stores', 'racks']):
        ax.plot(years, [BILLBOARD[y][key] for y in years], color='#384047', linestyle='--', label='Historical reference')
    for ax, title, ylabel in zip(axes.flat, ['Stores', 'All racks', 'Singles', 'Live LP repertoire'],
                                 ['LP units, millions', 'LP units, millions', 'Single units, millions', 'Mean live LP titles']):
        ax.set_title(title, loc='left', fontweight='bold')
        ax.set_ylabel(ylabel)
        ax.set_xticks(range(1960, 1970, 2))
        ax.grid(axis='y', alpha=.25)
        ax.spines[['top', 'right']].set_visible(False)
    title = 'LP calibration: retained fresh-seed decade' if rows.seed.nunique() == 1 else 'LP calibration: two fresh-seed decade runs'
    fig.suptitle(title, x=.08, ha='left', fontsize=21, fontweight='bold')
    fig.text(.08, .91, '2% LP intention reduction + 2% larger rack shopper pools; all candidate settings fixed.', color='#536573')
    handles, labels = axes[0, 0].get_legend_handles_labels()
    fig.legend(handles, labels, loc='lower center', ncol=3, frameon=False, bbox_to_anchor=(.5, .04))
    note = 'Complete settlement years only. Historical references end in 1965.'
    if rows.seed.nunique() == 1:
        note += ' Native run hit an end-date harness error; the requested two-seed matrix is incomplete.'
    fig.text(.08, .015, note, color='#536573', fontsize=9)
    fig.subplots_adjust(left=.08, right=.98, top=.85, bottom=.15, wspace=.22, hspace=.32)
    path = folder / 'lp-decade-holdout.png'
    fig.savefig(path, dpi=160, bbox_inches='tight', pad_inches=.2)
    plt.close(fig)
    print(path)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', default='lpdc-pool102-decade-holdout')
    parser.add_argument('--plot', action='store_true')
    parser.add_argument('--retained-seed', type=int, help='Explicitly adjudicate the documented single retained endpoint world')
    args = parser.parse_args()
    annual = review(args.tag, args.retained_seed)
    if args.plot:
        plot(ROOT / args.tag, annual)
