"""Small LP-intention screen and held-out-seed replication. Generated evidence stays in SimLogs."""
import argparse
import json
import os

import pandas as pd

from analyze_budget_lp_products import analyse
from analyze_single_capital_risk import ROOT


FACTORS = {'budgetlp100': 1., 'budgetlp098': .98, 'budgetlp096': .96}


def review(tags, provisional=False):
    results, rows = [], []
    for tag in tags:
        folder = ROOT / tag
        plan = json.loads((folder / 'plan.json').read_text(encoding='utf-8-sig'))
        if not set(plan['arms']) <= set(FACTORS):
            raise ValueError('Unknown calibration arm')
        for run in plan['runs']:
            flags = run['args']
            def value(prefix, fallback):
                values = [float(a[len(prefix):]) for a in flags if a.startswith(prefix)]
                if len(values) > 1:
                    raise ValueError('Duplicate setting: ' + prefix)
                return values[0] if values else fallback
            if not {'--rack-jobbers', '--budget-lps', '--rack-buyer-access', '--rack-mixed-assortment'} <= set(flags) or \
                value('--lp-demand-scale=', 1) != FACTORS[run['arm']] or value('--rack-store-choice=', 0) != 1 or \
                value('--rack-turnover=', 30) != 45 or value('--rack-shopper-scale=', .026) != .026 or \
                value('--budget-shopper-scale=', .08) != .08 or value('--budget-substitution=', .25) != .25 or \
                value('--budget-price=', 1.75) != 1.75:
                raise ValueError('Non-demand setting changed: ' + run['name'])
        result = analyse(folder / 'plan.json', provisional)
        results.append(result)
        rows.extend(dict(r, tag=tag, factor=FACTORS[r['arm']]) for r in result['annual'])
    if len({r['assemblySha256'] for r in results}) != 1:
        raise ValueError('Do not combine different assemblies')
    annual = pd.DataFrame(rows)
    if annual.duplicated(['arm', 'seed', 'year']).any():
        raise ValueError('Duplicate scenario/seed/year')
    annual['storesAndRacksM'] = annual.storesM + annual.allRacksM
    annual['rackSharePct'] = annual.allRacksM / annual.storesAndRacksM * 100
    annual['rawRackDemandM'] = annual.frontDemandM + annual.budgetDemandM
    annual['unfulfilledRackDemandM'] = annual.rawRackDemandM - annual.allRacksM
    annual['meanChannelErrorPct'] = (annual.storeErrorM.abs() / 68 + annual.rackErrorM.abs() / 60) * 50
    # This objective applies only to the complete 1961 year and never rescales recorded sales.
    annual.loc[(annual.year != 1961) | annual.partial, 'meanChannelErrorPct'] = None
    screen = annual[(annual.seed == 1001) & (annual.year == 1961) & ~annual.partial]
    candidate = None
    if set(screen.arm) == set(FACTORS):
        candidate = screen[screen.factor < 1].sort_values(['meanChannelErrorPct', 'factor'], ascending=[True, False]).iloc[0].arm
    paired = []
    for (seed, year), group in annual.groupby(['seed', 'year']):
        if group.observedWeeks.nunique() != 1:
            raise ValueError('Unequal settlement coverage')
        base = group[group.factor == 1]
        if len(base) != 1:
            continue
        for _, row in group[group.factor < 1].iterrows():
            paired.append(dict(seed=int(seed), year=int(year), partial=bool(row.partial), arm=row.arm,
                               **{m + 'Delta': float(row[m] - base.iloc[0][m]) for m in
                                  ['storesM', 'allRacksM', 'storesAndRacksM', 'rackSharePct', 'frontSwitchedM',
                                   'switchedStoreM', 'autonomousFrontUnitsM', 'rawRackDemandM', 'singlesM', 'meanLiveLps']}))
    result = dict(assemblySha256=results[0]['assemblySha256'], matrixComplete=all(r['matrixComplete'] for r in results),
                  calibrationAccepted=False, candidate=candidate, selectionSeed=1001,
                  selectionRule='Lowest mean absolute percentage error of stores and racks in complete 1961; ties prefer smaller reduction.',
                  checks=[c for r in results for c in r['checks']],
                  annual=annual.astype(object).where(pd.notna(annual), None).to_dict('records'), paired=paired,
                  warning='Only autonomous intentions scale. Transfers follow the reduced store cohort once. Independent worlds include feedback; partial years are not annualized. Clubs stay authored and singles have no direct multiplier.')
    if len(tags) > 1:
        held = annual[(annual.seed.isin([1002, 1003])) & (annual.year == 1961) & ~annual.partial]
        expected = {(s, a) for s in [1002, 1003] for a in ['budgetlp100', candidate]}
        observed = set(zip(held.seed, held.arm))
        result['replicationComplete'] = candidate is not None and expected <= observed
        other = (set(FACTORS) - {'budgetlp100', candidate}) if candidate else set()
        secondary = {(s, a) for s in [1002, 1003] for a in other}
        result['secondaryComplete'] = bool(secondary) and secondary <= observed
        if result['matrixComplete'] and not result['replicationComplete']:
            raise ValueError('Replication must contain the preselected candidate and unchanged case on seeds 1002 and 1003')
        if result['matrixComplete'] and observed.intersection(secondary) and not result['secondaryComplete']:
            raise ValueError('Secondary sensitivity must cover both held-out seeds')
    else:
        result['replicationComplete'] = False
        result['secondaryComplete'] = False
    return result, annual


def plot(folder, annual, candidate):
    os.environ['MPLCONFIGDIR'] = str(folder / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 12})
    rows = annual[(annual.year == 1961) & ~annual.partial]
    fig, axes = plt.subplots(1, 2, figsize=(13, 6))
    for seed, color, marker in [(1001, '#2675a6', 'o'), (1002, '#d77b18', 's'), (1003, '#48a6a4', '^')]:
        group = rows[rows.seed == seed]
        for ax, metric in zip(axes, ['storesM', 'allRacksM']):
            ax.scatter(group.factor, group[metric], color=color, marker=marker, s=85, label=f'Seed {seed}', zorder=3)
    for ax, metric, title, target in zip(axes, ['storesM', 'allRacksM'], ['Stores', 'All racks'], [68, 60]):
        ax.axhline(target, color='#384047', linestyle='--', label='Historical target')
        ax.set_title(title, loc='left', fontweight='bold', fontsize=17)
        ax.set_ylabel('LP units, millions')
        ax.set_xticks([.96, .98, 1], ['4% reduction', '2% reduction', 'Unchanged'])
        ax.set_xlim(.953, 1.007)
        ax.set_ylim(min(float(rows[metric].min()), target)-1, max(float(rows[metric].max()), target)+1)
        ax.text(1.006, target, f'{target}M', ha='right', va='bottom', fontsize=10, color='#384047')
        ax.grid(axis='y', alpha=.25)
        ax.spines[['top', 'right']].set_visible(False)
    handles, labels = axes[0].get_legend_handles_labels()
    fig.legend(handles, labels, loc='lower center', ncol=4, frameon=False, bbox_to_anchor=(.5, .06))
    fig.suptitle('1961: modest LP purchase-intention reductions', x=.07, ha='left', fontsize=21, fontweight='bold')
    fig.text(.07, .89, 'Buyer access 1, wider assortment and throughput 45 held fixed. Each point is a separate world.', color='#536573')
    reduction = (1 - FACTORS[candidate]) * 100
    note = (f'Seed 1001 selected the {reduction:g}% case; the other seeds replicate it. The alternate factor is exploratory.'
            if rows.seed.isin([1002, 1003]).any() else 'Selection uses seed 1001. The other seeds have not yet been checked.')
    fig.text(.07, .025, note + ' No default adopted.', fontsize=9, color='#536573')
    fig.subplots_adjust(left=.07, right=.98, top=.78, bottom=.25, wspace=.2)
    output = folder / 'lp-demand-calibration-1961.png'
    fig.savefig(output, dpi=170)
    print(output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tags', nargs='+', required=True)
    parser.add_argument('--output-tag')
    parser.add_argument('--completed-only', action='store_true')
    parser.add_argument('--plot', action='store_true')
    args = parser.parse_args()
    result, annual = review(args.tags, args.completed_only)
    folder = ROOT / (args.output_tag or args.tags[-1])
    folder.mkdir(exist_ok=True)
    suffix = '-provisional' if args.completed_only else ''
    (folder / f'lp-demand-calibration-review{suffix}.json').write_text(json.dumps(result, indent=2) + '\n')
    annual.to_csv(folder / f'lp-demand-calibration-annual{suffix}.csv', index=False)
    pd.DataFrame(result['paired']).to_csv(folder / f'lp-demand-calibration-paired{suffix}.csv', index=False)
    print(annual[annual.year == 1961][['seed', 'arm', 'factor', 'storesM', 'allRacksM', 'meanChannelErrorPct']].to_string(index=False))
    print('Preselected candidate:', result['candidate'])
    if args.plot:
        if not result['matrixComplete'] or args.completed_only:
            raise ValueError('Final plot requires completed evidence')
        plot(folder, annual, result['candidate'])
