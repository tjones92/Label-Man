"""Matched known-buyer access x choice share x throughput; no shopper-scale fitting."""
import argparse
import json
import os

import pandas as pd

from analyze_budget_lp_products import analyse
from analyze_single_capital_risk import ROOT


ARMS = {
    'budgetmixchoice': (False, .5, 30),
    'budgetmixchoice100': (False, 1, 30),
    'budgetmixaccess': (True, .5, 30),
    'budgetmixaccess100': (True, 1, 30),
    'budgetmixchoice45': (False, .5, 45),
    'budgetmixchoice100t45': (False, 1, 45),
    'budgetmixaccess45': (True, .5, 45),
    'budgetmixaccess100t45': (True, 1, 45),
}


def review(folder, provisional=False):
    plan = json.loads((folder / 'plan.json').read_text(encoding='utf-8-sig'))
    if set(plan['arms']) != set(ARMS):
        raise ValueError('Declare all eight matched access/share/throughput cells')
    for run in plan['runs']:
        flags = run['args']
        access, share, turnover = ARMS[run['arm']]

        def value(prefix, default):
            matches = [float(a[len(prefix):]) for a in flags if a.startswith(prefix)]
            if len(matches) > 1:
                raise ValueError('Duplicate scenario setting: ' + prefix)
            return matches[0] if matches else default

        if ('--rack-buyer-access' in flags) != access or '--rack-mixed-assortment' not in flags or \
            value('--rack-store-choice=', 0) != share or value('--rack-turnover=', 30) != turnover or \
            value('--rack-shopper-scale=', .026) != .026 or value('--budget-shopper-scale=', .08) != .08 or \
            value('--budget-price=', 1.75) != 1.75 or value('--budget-substitution=', .25) != .25:
            raise ValueError('Scenario differs from frozen experiment: ' + run['name'])
    result = analyse(folder / 'plan.json', provisional)
    annual = pd.DataFrame(result['annual'])
    annual['buyerAccess'] = annual.arm.map(lambda a: ARMS[a][0])
    annual['choiceShare'] = annual.arm.map(lambda a: ARMS[a][1])
    annual['turnover'] = annual.arm.map(lambda a: ARMS[a][2])
    annual['rawRackDemandM'] = annual.frontDemandM + annual.budgetDemandM
    annual['unfulfilledRackDemandM'] = annual.rawRackDemandM - annual.allRacksM
    annual['unfulfilledFrontPreferencesM'] = annual.frontPlannedM - annual.frontSwitchedM
    annual['storesAndRacksM'] = annual.storesM + annual.allRacksM
    metrics = ['storesM', 'storeBeforeChoiceM', 'allRacksM', 'frontRacksM', 'budgetRacksM', 'budgetDemandM',
               'frontHitM', 'frontCatalogueM', 'frontPlannedM', 'frontSwitchedM', 'switchedStoreM', 'shelfM', 'unusedShelfM',
               'totalSwitchedM', 'autonomousFrontUnitsM', 'autonomousFrontDemandM', 'rawRackDemandM',
               'unfulfilledRackDemandM', 'storesAndRacksM', 'meanLiveLps', 'lpRetirements', 'singlesM']
    effects = []
    for (seed, year), group in annual.groupby(['seed', 'year']):
        if group.observedWeeks.nunique() != 1:
            raise ValueError('Unequal settlement-calendar coverage')
        rows = {(bool(r.buyerAccess), r.choiceShare, int(r.turnover)): r for _, r in group.iterrows()}
        for key, row in rows.items():
            access, share, turnover = key
            comparisons = []
            if access:
                comparisons.append(('access', (False, share, turnover)))
            if share == 1:
                comparisons.append(('choiceShare', (access, .5, turnover)))
            if turnover == 45:
                comparisons.append(('throughput', (access, share, 30)))
            for effect, base_key in comparisons:
                if base_key not in rows:
                    continue
                base = rows[base_key]
                effects.append(dict(seed=int(seed), year=int(year), partial=bool(row.partial), effect=effect,
                                    arm=row.arm, baseline=base.arm,
                                    **{m + 'Delta': float(row[m] - base[m]) for m in metrics}))
    result.update(annual=annual.astype(object).where(pd.notna(annual), None).to_dict('records'), accessEffects=effects)
    result['assumptions']['warning'] += ' Access changes only already-stocked titles; reach remains fixed. Raw rack intentions can include unfulfilled transfers whose buyers remain in stores.'
    suffix = '-provisional' if provisional else ''
    (folder / f'buyer-access-review{suffix}.json').write_text(json.dumps(result, indent=2) + '\n')
    annual.to_csv(folder / f'buyer-access-annual{suffix}.csv', index=False)
    pd.DataFrame(effects).to_csv(folder / f'buyer-access-effects{suffix}.csv', index=False)
    return result, annual


def plot(folder, annual, year):
    rows = annual[(annual.year == year) & ~annual.partial]
    if len(rows) != 8 or rows.seed.nunique() != 1:
        raise ValueError('Plot requires one seed and eight complete-year cells')
    os.environ['MPLCONFIGDIR'] = str(folder / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from matplotlib.lines import Line2D
    from analyze_rack_jobber_probe import BILLBOARD

    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 12})
    fig, axes = plt.subplots(1, 2, figsize=(15, 7))
    profiles = [(False, .5), (False, 1), (True, .5), (True, 1)]
    labels = ['Display rule\nchoice 0.5', 'Display rule\nchoice 1.0', 'Buyer access\nchoice 0.5', 'Buyer access\nchoice 1.0']
    indexed = rows.set_index(['buyerAccess', 'choiceShare', 'turnover'])
    for ax, metric, title, target in zip(axes, ['storesM', 'allRacksM'], ['Stores', 'All racks'],
                                        [BILLBOARD[year]['stores'], BILLBOARD[year]['racks']]):
        for turnover, offset, color in [(30, -.19, '#2675a6'), (45, .19, '#d77b18')]:
            values = [float(indexed.loc[(access, share, turnover), metric]) for access, share in profiles]
            bars = ax.bar([i + offset for i in range(4)], values, width=.35, color=color,
                          label=f'{turnover} LPs per rack/week')
            ax.bar_label(bars, fmt='%.2f', padding=4, fontsize=11)
        ax.axhline(target, color='#384047', linestyle='--', linewidth=1.8)
        ax.set_title(title, loc='left', fontsize=17, fontweight='bold')
        ax.set_ylabel('LP units, millions')
        ax.set_xticks(range(4), labels)
        ax.set_ylim(0, max(float(rows[metric].max()), target) * 1.15)
        ax.text(3.6, target + .7, f'{target}M target', ha='right', fontsize=11, color='#384047',
                bbox=dict(facecolor='white', edgecolor='none', pad=1, alpha=.94))
        ax.grid(axis='y', alpha=.25)
        ax.set_axisbelow(True)
        ax.spines[['top', 'right']].set_visible(False)
    fig.suptitle(f'{year}: known-buyer access and rack throughput', x=.07, ha='left', fontsize=22, fontweight='bold')
    fig.text(.07, .89, f'Matched seed {int(rows.seed.iloc[0])}. Wider assortment, shopper scales, prices and reach held fixed.', color='#536573', fontsize=12)
    handles, legend_labels = axes[0].get_legend_handles_labels()
    fig.legend(handles + [Line2D([0], [0], color='#384047', linestyle='--')],
               legend_labels + ['Historical targets'], loc='lower center', ncol=3, frameon=False, bbox_to_anchor=(.5, .07))
    fig.text(.07, .025, 'Scenario screen; coefficients and throughput are assumptions. No accepted historical fit.', color='#536573', fontsize=10)
    fig.subplots_adjust(left=.07, right=.98, top=.79, bottom=.25, wspace=.16)
    output = folder / f'lp-buyer-access-{year}.png'
    fig.savefig(output, dpi=170)
    print(output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--completed-only', action='store_true')
    parser.add_argument('--plot-year', type=int)
    args = parser.parse_args()
    folder = ROOT / args.tag
    result, annual = review(folder, args.completed_only)
    print(annual[annual.year == 1961][['arm', 'observedWeeks', 'storesM', 'allRacksM', 'frontPlannedM', 'frontSwitchedM', 'rawRackDemandM']].to_string(index=False))
    if args.plot_year:
        if not result['matrixComplete'] or args.completed_only:
            raise ValueError('Plot requires final complete evidence')
        plot(folder, annual, args.plot_year)
