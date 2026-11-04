"""Plot completed factorial cells with the fulfilled store-to-rack flow visible."""
import argparse
import json
import os

import numpy as np
import pandas as pd

from analyze_rack_jobber_probe import BILLBOARD
from analyze_single_capital_risk import ROOT

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--year', type=int, default=1961)
    args = parser.parse_args()
    folder = ROOT / args.tag
    review = json.loads((folder / 'factorial-review.json').read_text())
    if not review['matrixComplete']:
        raise ValueError('Complete the declared matrix before plotting')
    rows = pd.DataFrame(review['annual'])
    rows = rows[(rows.year == args.year) & ~rows.partial]
    order = ['budget', 'budgetmix', 'budgetchoice', 'budgetmixchoice']
    if set(rows.arm) != set(order) or len(rows) != 4:
        raise ValueError('One seed and four complete factorial cells required')
    rows = rows.set_index('arm').loc[order]
    os.environ['MPLCONFIGDIR'] = str(folder / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from matplotlib.lines import Line2D
    from matplotlib.patches import Patch
    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 11})
    blue, teal, orange, grey = '#2675a6', '#48a6a4', '#d77b18', '#384047'
    figure, axes = plt.subplots(1, 2, figsize=(12, 6))
    x = np.arange(4)
    labels = ['Current\nbudget model', 'Wider\nassortment', 'Broader\nchannel choice', 'Both\ntreatments']
    axes[0].bar(x, rows.storesM, color=blue, width=.62)
    axes[1].bar(x, rows.autonomousFrontUnitsM, color=blue, width=.62)
    axes[1].bar(x, rows.frontSwitchedM, bottom=rows.autonomousFrontUnitsM, color=teal, width=.62)
    axes[1].bar(x, rows.budgetRacksM, bottom=rows.frontRacksM, color=orange, width=.62)
    for axis, title, values, target in zip(axes, ['Stores', 'All racks'], [rows.storesM, rows.allRacksM],
                                         [BILLBOARD[args.year]['stores'], BILLBOARD[args.year]['racks']]):
        axis.axhline(target, color=grey, linestyle='--', linewidth=1.8)
        axis.set_title(title, loc='left', fontweight='bold', fontsize=16)
        axis.set_ylabel('LP units, millions')
        axis.set_xticks(x, labels)
        axis.set_ylim(0, max(float(values.max()), target) * 1.15)
        axis.grid(axis='y', color='#e1e5e8'); axis.set_axisbelow(True)
        axis.spines[['top', 'right']].set_visible(False)
        for i, value in enumerate(values):
            axis.text(i, value + .8, f'{value:.1f}', ha='center')
        axis.text(3.48, target + .6, f'{target}M target', ha='right', fontsize=9, color=grey,
                  bbox=dict(facecolor='white', edgecolor='none', pad=1, alpha=.94))
    figure.suptitle(f'{args.year}: assortment × buyer channel choice', x=.07, ha='left', fontsize=21, fontweight='bold')
    figure.text(.07, .90, 'Matched seed 1001. Shopper scales, prices and rack unit capacity held fixed.', color='#536573')
    handles = [Patch(color=blue, label='Autonomous front-line racks'), Patch(color=teal, label='Store buyers routed to front-line racks'),
               Patch(color=orange, label='Budget racks'), Line2D([0], [0], color=grey, linestyle='--', label='Historical target')]
    figure.legend(handles=handles, loc='lower center', bbox_to_anchor=(.5, .075), ncol=2, frameon=False, fontsize=10)
    figure.text(.07, .025, f'All-rack shelf ceiling: {rows.shelfM.iloc[0]:.2f}M. Trial assumptions; no accepted historical fit.', color='#536573', fontsize=10)
    figure.subplots_adjust(left=.07, right=.98, top=.78, bottom=.27, wspace=.16)
    output = folder / f'lp-channel-factorial-{args.year}.png'
    figure.savefig(output, dpi=170, bbox_inches='tight')
    print(output)
