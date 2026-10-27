"""Compare the .98 LP case with a matched increase in both rack shopper pools."""
import argparse
import hashlib
import json
import os

import pandas as pd

from analyze_budget_lp_products import analyse
from analyze_single_capital_risk import ROOT


BASE = 'budgetlp098'
POOLS = {
    'budgetlp098pool102': (2, '0.02652', '0.0816'),
    'budgetlp098pool105': (5, '0.0273', '0.084'),
}
SEEDS = {1001, 1002, 1003}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_plan(tag):
    plan = json.loads((ROOT / tag / 'plan.json').read_text(encoding='utf-8-sig'))
    expected = {(arm, seed) for arm in plan['arms'] for seed in plan['seeds']}
    observed = [(r['arm'], r['seed']) for r in plan['runs'] if r['passed']]
    require(len(observed) == len(expected) and set(observed) == expected,
            'Source matrix must be complete: ' + tag)
    require(plan['weeks'] == 106 and plan['checks'] and all(c['passed'] for c in plan['checks']),
            'Require 106 weeks and passed source checks: ' + tag)
    return plan


def settings(run):
    # Scene/log destinations are positional. Runtime identity flags differ by run.
    flags = [a for a in run['args'] if a.startswith('--') and a != '--log-file'
             and not a.startswith(('--run=', '--seed='))]
    keys = [a.split('=')[0] for a in flags if '=' in a]
    require(len(keys) == len(set(keys)), 'Duplicate parameter: ' + run['name'])
    return set(flags)


def review(tag, baseline_tags):
    folder = ROOT / tag
    treatment = read_plan(tag)
    require(len(treatment['arms']) == 1 and treatment['arms'][0] in POOLS and set(treatment['seeds']) == SEEDS,
            'Declare exactly three worlds with one supported pool increase')
    lift_arm = treatment['arms'][0]
    increase, front_scale, budget_scale = POOLS[lift_arm]
    baselines = [read_plan(t) for t in baseline_tags]
    plans = [treatment] + baselines
    require(len({p['assemblySha256'] for p in plans}) == 1, 'Assembly mismatch')
    digest = hashlib.sha256((ROOT.parent / '.godot/mono/temp/bin/Debug/Label Man.dll').read_bytes()).hexdigest().upper()
    require(digest == treatment['assemblySha256'], 'Current assembly differs from the run assembly')
    base_runs = [dict(r, sourceTag=t) for t, p in zip(baseline_tags, baselines)
                 for r in p['runs'] if r['arm'] == BASE and r['seed'] in SEEDS]
    require(len(base_runs) == 3 and {r['seed'] for r in base_runs} == SEEDS,
            'Need one archived .98 comparison for each seed')
    required = {'--lp-demand-scale=0.98', '--rack-turnover=45', '--rack-store-choice=1',
                '--budget-price=1.75', '--rack-buyer-access', '--rack-mixed-assortment',
                '--rack-jobbers', '--budget-lps', '--record-clubs', '--weeks=106'}
    for run in treatment['runs']:
        base = next(r for r in base_runs if r['seed'] == run['seed'])
        before, after = settings(base), settings(run)
        require(required <= before and required <= after, 'Frozen setting changed')
        require(before - after == {'--budget-shopper-scale=0.08'} and
                after - before == {'--budget-shopper-scale=' + budget_scale, '--rack-shopper-scale=' + front_scale},
                'Only the two rack pools may differ: ' + run['name'])
    combined = dict(treatment, arms=[BASE, lift_arm], runs=base_runs + treatment['runs'],
                    checks=[dict(c, sourceTag=t) for t, p in [(tag, treatment)] + list(zip(baseline_tags, baselines))
                            for c in p['checks']], baselineTags=baseline_tags)
    comparison_path = folder / 'comparison-plan.json'
    comparison_path.write_text(json.dumps(combined, indent=2) + '\n')
    result = analyse(comparison_path)
    annual = pd.DataFrame(result['annual'])
    annual['rawRackDemandM'] = annual.frontDemandM + annual.budgetDemandM
    annual['unfulfilledRackDemandM'] = annual.rawRackDemandM - annual.allRacksM
    annual['storesAndRacksM'] = annual.storesM + annual.allRacksM
    annual['meanChannelErrorPct'] = (abs(annual.storesM / 68 - 1) + abs(annual.allRacksM / 60 - 1)) * 50
    annual.loc[(annual.year != 1961) | annual.partial, 'meanChannelErrorPct'] = None
    target = annual[(annual.year == 1961) & ~annual.partial]
    require(len(target) == 6 and target.observedWeeks.eq(52).all(), 'Need six complete 1961 years')
    metrics = ['storesM', 'frontRacksM', 'budgetRacksM', 'allRacksM', 'storesAndRacksM',
               'rawRackDemandM', 'unfulfilledRackDemandM', 'frontSwitchedM', 'switchedStoreM',
               'autonomousFrontUnitsM', 'shelfM', 'singlesM', 'clubsM', 'meanLiveLps', 'meanChannelErrorPct']
    paired = []
    for seed in sorted(SEEDS):
        group = target[target.seed == seed].set_index('arm')
        base, lift = group.loc[BASE], group.loc[lift_arm]
        paired.append(dict(seed=seed, **{m + 'Delta': float(lift[m] - base[m]) for m in metrics},
                           rackLiftPct=float((lift.allRacksM / base.allRacksM - 1) * 100)))
    result.update(annual=annual.astype(object).where(pd.notna(annual), None).to_dict('records'), paired=paired,
                  means1961=target.groupby('arm')[metrics].mean().reset_index().to_dict('records'),
                  sourceTags=[tag] + baseline_tags, poolIncreasePct=increase, calibrationAccepted=False,
                  scope='Three paired worlds, complete 1961 settlement years. Prior .98 controls reuse the same assembly. '
                        '1960 and 1962 totals are reported separately; partial years are not annualized. '
                        f'The {increase}% treatment was chosen after inspecting these seeds; this is exploratory, not an independent holdout.')
    (folder / 'rack-pool-review.json').write_text(json.dumps(result, indent=2) + '\n')
    annual.to_csv(folder / 'rack-pool-annual.csv', index=False)
    pd.DataFrame(paired).to_csv(folder / 'rack-pool-paired-1961.csv', index=False)
    print(target[['seed', 'arm', 'storesM', 'allRacksM', 'meanChannelErrorPct']].sort_values(['seed', 'arm']).to_string(index=False))
    print(pd.DataFrame(paired)[['seed', 'storesMDelta', 'allRacksMDelta', 'rackLiftPct', 'meanChannelErrorPctDelta']].to_string(index=False))
    print('Means:', json.dumps(result['means1961']))
    return target


def plot(folder, rows):
    treatments = set(rows.arm) - {BASE}
    require(len(treatments) == 1 and treatments <= set(POOLS), 'Plot requires one supported pool treatment')
    lift_arm = next(iter(treatments))
    increase = POOLS[lift_arm][0]
    os.environ['MPLCONFIGDIR'] = str(folder / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 12})
    fig, axes = plt.subplots(1, 2, figsize=(12, 5.6))
    for seed, color, marker in [(1001, '#2675a6', 'o'), (1002, '#d77b18', 's'), (1003, '#48a6a4', '^')]:
        group = rows[rows.seed == seed].set_index('arm').loc[[BASE, lift_arm]]
        for ax, metric in zip(axes, ['storesM', 'allRacksM']):
            ax.plot([0, 1], group[metric], color=color, marker=marker, markersize=8,
                    linewidth=1.5, label=f'Seed {seed}')
    for ax, metric, title, target in zip(axes, ['storesM', 'allRacksM'], ['Stores', 'All racks'], [68, 60]):
        ax.axhline(target, color='#384047', linestyle='--', label=f'{target}M reference')
        ax.text(1.13, target, f'{target}M reference', ha='right', va='bottom', color='#384047', fontsize=10)
        ax.set_title(title, loc='left', fontweight='bold', fontsize=17)
        ax.set_ylabel('LP units, millions')
        ax.set_xticks([0, 1], ['2% reduction', f'+{increase}% rack pools'])
        ax.set_xlim(-.15, 1.15)
        ax.set_ylim(min(float(rows[metric].min()), target) - .7, max(float(rows[metric].max()), target) + .7)
        ax.grid(axis='y', alpha=.25)
        ax.spines[['top', 'right']].set_visible(False)
    fig.suptitle(f'1961: a {increase}% increase in both rack shopper pools', x=.08, ha='left', fontweight='bold', fontsize=19)
    fig.text(.08, .89, '2% LP intention reduction retained; prices, assortment, buyer access and throughput held fixed.', color='#536573', fontsize=11)
    handles, labels = axes[0].get_legend_handles_labels()
    fig.legend(handles[:3], labels[:3], loc='lower center', ncol=3, frameon=False, bbox_to_anchor=(.5, .07))
    fig.text(.08, .025, 'Complete 1961 years from separate simulated worlds. Exploratory comparison; no default adopted.', color='#536573', fontsize=9)
    fig.subplots_adjust(left=.08, right=.98, top=.78, bottom=.28, wspace=.22)
    path = folder / 'rack-pool-1961.png'
    fig.savefig(path, dpi=170, bbox_inches='tight', pad_inches=.2)
    plt.close(fig)
    print(path)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--baseline-tags', nargs='+', default=['lpdc-v2-screen', 'lpdc-v2-secondary'])
    parser.add_argument('--plot', action='store_true')
    args = parser.parse_args()
    rows = review(args.tag, args.baseline_tags)
    if args.plot:
        plot(ROOT / args.tag, rows)
