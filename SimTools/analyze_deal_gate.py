"""--proven-hit-deal-weeks screen (LpGenreAllocationFollowup.md §32). Usage from the repo root:
python SimTools/analyze_deal_gate.py CONTROL ARM [ARM ...] [--seeds 2002 1001 3003]
Per run and seed: share of starting Small / Boutique / Independent labels with a signed P&D deal by the end of 1960 and
1961 (target 5-15% a year for Small), share with a wholesale house placement and placements, AI master leases (all /
Southern sellers), Southern small labels' home fraction of singles (1961), and the label-city screen's chart, closure
and genre rows (analyze_label_city_weights.py)."""
import sys, re, subprocess, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001', '3003']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
for run in args:
    print(f'== {run}')
    for seed in seeds:
        p = f'SimLogs/{run}-{seed}-sc-'
        d = pd.read_csv(p + 'label-directory.csv'); geo = pd.read_csv(p + 'label-geography.csv', usecols=['labelId', 'homeRegion'])
        st = d[(~d.isHistorical) & d.initialTier.isin(['Small', 'Boutique', 'Independent'])].merge(geo, on='labelId')
        sg = pd.read_csv(p + 'deal-ledger.csv'); sg = sg[sg.resolution == 'Signed']
        ie = pd.read_csv(p + 'independent-distribution-events.csv')
        rows = []
        for y in (1960, 1961):
            parts = []
            for tier in ('Small', 'Boutique', 'Independent'):
                ids = set(st[st.initialTier == tier].labelId)
                s = sg[(sg.year <= y) & sg.clientId.isin(ids)].clientId.nunique()
                h = ie[(ie.year <= y) & ie.labelId.isin(ids)]
                parts.append(f'{tier[:5]} P&D {100 * s / len(ids):.0f}% house {100 * h.labelId.nunique() / len(ids):.0f}% ({len(h)})')
            rows.append(f'{y}: ' + ', '.join(parts))
        log = open(f'SimLogs/writing-calibration/{run}-{seed}-sc.log', encoding='utf-8', errors='ignore').read()
        sellers = re.findall(r'AI_MASTER_DEAL week=\d+ route=\w+ kind=\w+ seller=(label_\d+|gen_\S+)', log)
        south = set(geo[geo.homeRegion == 'deepsouth'].labelId)
        rows.append(f'leases {len(sellers)} (Southern sellers {sum(s in south for s in sellers)})')
        print(f'  {seed}: ' + ' | '.join(rows))
    subprocess.run([sys.executable, 'SimTools/analyze_label_city_weights.py', run, '--seeds', *seeds])
