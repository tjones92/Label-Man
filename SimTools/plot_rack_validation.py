"""Plot a completed, combined rack historical screen. Outputs stay under SimLogs.

Requires pandas and matplotlib.
python SimTools/plot_rack_validation.py --tag rkj-returns-v2
"""
import argparse
import os
from pathlib import Path

import pandas as pd

from analyze_rack_jobber_probe import BILLBOARD


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag', required=True)
    args = parser.parse_args()
    output = Path(__file__).resolve().parents[1] / 'SimLogs' / args.tag / 'review'
    table = pd.read_csv(output / 'annual.csv')
    table = table[table.completeYear & table.seed.eq(1001) & table.arm.isin(['control', 'racks'])]
    table = table.sort_values('durationWeeks').drop_duplicates(['arm', 'year'], keep='last')
    if not all((table.arm.eq(arm) & table.year.eq(1965)).any() for arm in ('control', 'racks')):
        raise ValueError('Need completed control and rack growth runs through 1965')
    os.environ['MPLCONFIGDIR'] = str(output / 'mpl-cache')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt

    plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 11, 'axes.spines.top': False,
                         'axes.spines.right': False, 'axes.titleweight': 'bold'})
    figure, axes = plt.subplots(1, 3, figsize=(13.5, 4.5), constrained_layout=False)
    blue, orange, grey = '#236B9D', '#CE731E', '#353D43'
    for ax, title, metric, historic in zip(axes, ['Stores', 'Racks', 'Clubs and mail'],
                                         ['storeLpM', 'rackLpM', 'clubLpM'], ['stores', 'racks', 'clubs']):
        for arm, color, label in [('control', blue, 'Clubs control'), ('racks', orange, 'With front-line racks')]:
            if historic == 'racks' and arm == 'control':
                continue
            if historic == 'clubs' and arm == 'control':
                continue
            rows = table[table.arm.eq(arm)].sort_values('year')
            ax.plot(rows.year, rows[metric], color=color, marker='o', linewidth=2,
                    label='Model (authored volume)' if historic == 'clubs' else label)
        ax.plot(list(BILLBOARD), [BILLBOARD[y][historic] for y in BILLBOARD], color=grey,
                linestyle='--', marker='s', markersize=4, label='Historical (includes budget)' if historic == 'racks' else 'Historical')
        ax.set_title(title, loc='left')
        ax.set_ylabel('LP units, millions')
        ax.set_xticks(range(1960, 1966))
        ax.set_ylim(bottom=0)
        ax.grid(axis='y', color='#DDE2E6', linewidth=.7)
        ax.set_axisbelow(True)
        ax.legend(frameon=False, fontsize=9, loc='best')
    figure.suptitle('LP channels: frozen rack scale, seed 1001', x=.06, ha='left', fontsize=17, weight='bold')
    figure.text(.06, .035, 'Billboard 1966 guide, older 1961–65 series. Model racks exclude budget LPs. '
                'All model channels use the settlement calendar.', fontsize=9, color='#53616C')
    figure.subplots_adjust(left=.06, right=.985, bottom=.18, top=.82, wspace=.29)
    figure.savefig(output / 'lp-channel-growth.png', dpi=160, facecolor='white')
    figure.savefig(output / 'lp-channel-growth.svg', facecolor='white')
    plt.close(figure)
    print(output / 'lp-channel-growth.png')


if __name__ == '__main__':
    main()
