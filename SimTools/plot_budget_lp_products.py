"""Plot a completed budget comparison; partial years are excluded."""
import argparse
import json
import os
from pathlib import Path

import numpy as np

from analyze_rack_jobber_probe import BILLBOARD
from analyze_single_capital_risk import ROOT


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--year', type=int, default=1961)
    parser.add_argument('--seed', type=int, default=1001)
    args = parser.parse_args()
    folder = ROOT / args.tag
    os.environ['MPLCONFIGDIR'] = str(folder / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    review_path = folder / 'budget-review.json'
    if not review_path.exists():
        review_path = folder / 'factorial-review.json'
    review = json.loads(review_path.read_text())
    if not review['matrixComplete']:
        raise ValueError('The declared comparison matrix must be complete')
    rows = [r for r in review['annual'] if r['year'] == args.year and r['seed'] == args.seed and not r['partial']]
    if not rows or args.year not in BILLBOARD:
        raise ValueError('A completed historical comparison year is required')
    order = ['racks', 'budget', 'budgetmix', 'budgetchoice', 'budgetmixchoice', 'budgetadd', 'budgetturnover45', 'budgetprice125']
    rows.sort(key=lambda row: order.index(row['arm']) if row['arm'] in order else len(order))
    names = {'racks': 'Front-line\nonly', 'budget': 'Budget +\nsubstitution', 'budgetadd': 'Budget,\nweekly switch off',
             'budgetturnover45': 'Budget +\nturnover 45', 'budgetprice125': '$1.25 +\nmargin gate',
             'budgetmix': 'Wider\nassortment', 'budgetchoice': 'Broader\nchannel choice', 'budgetmixchoice': 'Both\ntreatments'}
    if any(r['arm'] == 'budgetmix' for r in rows):
        names['budget'] = 'Current\nbudget model'
    x = np.arange(len(rows))
    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 11})
    fig, axes = plt.subplots(1, 2, figsize=(12, 5.4))
    stores = [r['storesM'] for r in rows]
    front = [r['frontRacksM'] for r in rows]
    budget = [r['budgetRacksM'] for r in rows]
    axes[0].bar(x, stores, color='#2675a6', width=.65)
    axes[1].bar(x, front, color='#2675a6', width=.65, label='Front-line')
    axes[1].bar(x, budget, bottom=front, color='#d77b18', width=.65, label='Budget')
    for axis, title, target in zip(axes, ['Stores', 'All racks'], [BILLBOARD[args.year]['stores'], BILLBOARD[args.year]['racks']]):
        axis.axhline(target, color='#384047', linestyle='--', linewidth=1.8, label=f'Historical: {target}M')
        axis.set_xticks(x, [names.get(r['arm'], r['arm']) for r in rows], fontsize=9)
        axis.set_title(title, loc='left', fontweight='bold', fontsize=16)
        axis.set_ylabel('LP units, millions')
        axis.set_ylim(bottom=0)
        axis.grid(axis='y', color='#e1e5e8')
        axis.set_axisbelow(True)
        axis.spines[['top', 'right']].set_visible(False)
        axis.legend(frameon=False, loc='lower right', fontsize=9)
    for axis, values in zip(axes, [stores, [a + b for a, b in zip(front, budget)]]):
        for index, value in enumerate(values):
            axis.text(index, value + .8, f'{value:.1f}', ha='center', fontsize=10)
    fig.suptitle(f'{args.year} LP channels: budget products and shared shelf', x=.07, ha='left', fontweight='bold', fontsize=19)
    fig.text(.07, .91, f'Matched seed {args.seed}; front-line shopper scale frozen at .026; trial assumptions, no accepted fit.', color='#536573')
    fig.text(.07, .02, 'One settlement calendar. Shelf and assortment shared. Own-master products and synthetic opening baskets.', color='#536573', fontsize=10)
    fig.tight_layout(rect=(.01, .06, 1, .87))
    output = folder / f'budget-channels-{args.year}-{args.seed}.png'
    fig.savefig(output, dpi=170, bbox_inches='tight')
    print(output)
