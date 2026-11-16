"""Model LP sales distribution against the RIAA / Glover 1965 release survey (RIAA-1965-LP-sales-research-notes.md).

RIAA: 1,603 non-classical LPs released in 1963 by six companies (Capitol, Columbia, RCA Victor, Mercury, Laurie, Kapp),
sales counted through the end of 1964 (12-24 months). Model analogue: LPs launched weeks 1-52, units at week 104, so run
104-week worlds. Classical excluded, opening catalogue excluded. Units = store units (release-outcomes pressingBookSold /
live-records-snapshot totalUnitsSold) and, separately, store + rack (rack-title-weekly). Record clubs are logged by label
only, so they are not in a title's count (RIAA manufacturer sales would include club sales).

Run from SimLogs/: python ../SimTools/analyze_lp_riaa_1965.py [--compact] [--launch=105-156 --last=208] run ...
--launch=A-B picks LPs dropped in weeks A-B (default 1-52); --last caps rack weeks (default 104). At 208 weeks use
--launch=105-156 --last=208: 1962 releases with sales to end-1963, the RIAA window.
"""
import sys
import pandas as pd

BANDS = [0, 2000, 4000, 8000, 10000, 20000, 50000, 100000, 300000, 600000, 1000000, 1e12]
LABELS = ['0-2k', '2-4k', '4-8k', '8-10k', '10-20k', '20-50k', '50-100k', '100-300k', '300-600k', '600k-1M', '1M+']
RIAA = [574, 209, 206, 68, 171, 196, 98, 60, 12, 2, 7]  # p. 863


def cohort(t, last_week):
    ro = pd.read_csv(f'{t}-release-outcomes.csv', usecols=['recordId', 'format', 'pressingBookSold', 'realizedNet'])
    lv = pd.read_csv(f'{t}-live-records-snapshot.csv'); lv = lv[lv.week == lv.week.max()]
    u = pd.concat([ro.rename(columns={'pressingBookSold': 'units', 'realizedNet': 'net'})[['recordId', 'format', 'units', 'net']],
                   lv.rename(columns={'totalUnitsSold': 'units', 'observedNetLowerBound': 'net'})[['recordId', 'format', 'units', 'net']]]
                  ).drop_duplicates('recordId', keep='last').set_index('recordId')
    cen = pd.read_csv(f'{t}-demand-release-census.csv', usecols=['recordId', 'artistId', 'genre', 'observationKind', 'launchTier'])
    cen = cen[cen.observationKind == 'Launch'].drop_duplicates('recordId')
    pr = pd.read_csv(f'{t}-album-projects.csv', usecols=['albumRecordId', 'dropWeek', 'careerStateAtSchedule', 'tierAtSchedule'])
    pr = pr.dropna(subset=['albumRecordId']).drop_duplicates('albumRecordId').set_index('albumRecordId')
    lp = cen.join(u, on='recordId', how='inner')
    lp = lp[lp.format == 'Album'].join(pr, on='recordId', how='inner')
    # first in-run LP per act (by drop week) among all launched LPs
    lp = lp.sort_values('dropWeek')
    lp['firstLp'] = ~lp.artistId.duplicated() & lp.artistId.notna()
    rk = pd.read_csv(f'{t}-rack-title-weekly.csv', usecols=['week', 'recordId', 'units'])
    lp['rack'] = lp.recordId.map(rk[rk.week <= last_week].groupby('recordId').units.sum()).fillna(0)
    lp['storeRack'] = lp.units + lp.rack
    lp['tier'] = lp.launchTier.fillna(lp.tierAtSchedule)
    lp = lp[(lp.dropWeek >= LAUNCH[0]) & (lp.dropWeek <= LAUNCH[1]) & (lp.genre != 'Classical')]
    lp['debut'] = lp.firstLp & (lp.careerStateAtSchedule == 'NewSigning')
    return lp


def dist(s):
    c = pd.cut(s, BANDS, right=False, labels=LABELS).value_counts().reindex(LABELS)
    return c / c.sum() * 100


def summary(g, col):
    s = g[col]
    return {'n': len(s), 'median k': round(s.median() / 1e3, 1), 'mean k': round(s.mean() / 1e3, 1),
            '<4k %': round(100 * (s < 4000).mean(), 1), '<8k %': round(100 * (s < 8000).mean(), 1),
            '<7.8k %': round(100 * (s < 7800).mean(), 1), 'losing %': round(100 * (g.net < 0).mean(), 1)}


def compact(t):
    """One column per run: RIAA-band metrics by tier group, plus channel totals and bankruptcies."""
    import re
    lp = cohort(t, LAST)
    o = {}
    for name, g in (('all', lp), ('Major', lp[lp.tier == 'Major']), ('Major debut', lp[lp.debut & (lp.tier == 'Major')]),
                    ('Major non-debut', lp[~lp.debut & (lp.tier == 'Major')]), ('MidTier', lp[lp.tier == 'MidTier']),
                    ('Indep+Small', lp[lp.tier.isin(['Independent', 'Small'])])):
        s = g.storeRack
        o[f'{name}: n'] = len(s)
        o[f'{name}: <4k / <8k %'] = '%.0f / %.0f' % (100 * (s < 4000).mean(), 100 * (s < 8000).mean())
        o[f'{name}: median / mean k'] = '%.1f / %.1f' % (s.median() / 1e3, s.mean() / 1e3)
        o[f'{name}: 300k+ %'] = round(100 * (s >= 300000).mean(), 1)
        o[f'{name}: units M'] = round(s.sum() / 1e6, 2)
    cen = pd.read_csv(f'{t}-demand-release-census.csv', usecols=['recordId', 'labelId', 'format', 'observationKind', 'launchTier', 'observedYear'])
    cen = cen[(cen.observationKind == 'Launch') & (cen.format == 'Album') & (cen.launchTier == 'Major')].drop_duplicates('recordId')
    for y in sorted(cen.observedYear.unique()):
        c = cen[cen.observedYear == y]
        o[f'Major LPs per label {y}'] = round(len(c) / max(1, c.labelId.nunique()), 1)
    try:
        g = pd.read_csv(f'{t}-release-profit-gates.csv', usecols=['labelId', 'careerState', 'strategy', 'commit'])
        majors = set(cen.labelId)
        g = g[(g.strategy != 'OrphanSingle') & g.labelId.isin(majors)]
        o['Major LP gate pass % (all / NewSigning)'] = '%.0f / %.0f' % (100 * g.commit.mean(), 100 * g[g.careerState == 'NewSigning'].commit.mean())
    except FileNotFoundError:
        pass
    o['Major share of cohort units %'] =round(100 * lp[lp.tier == 'Major'].storeRack.sum() / lp.storeRack.sum(), 1)
    o['all: log-sd'] = round(np.log(lp.storeRack.clip(lower=1)).std(), 2)
    o['Major: log-sd'] = round(np.log(lp[lp.tier == 'Major'].storeRack.clip(lower=1)).std(), 2)
    o['all: losing %'] = round(100 * (lp.net < 0).mean(), 1)
    ru = pd.read_csv(f'{t}-decade-annual-rollup.csv', usecols=['year', 'albumUnits']).set_index('year').albumUnits
    rk = pd.read_csv(f'{t}-rack-title-weekly.csv', usecols=['year', 'units']).groupby('year').units.sum()
    cm = pd.read_csv(f'{t}-company-monthly.csv', usecols=['year', 'recordGross']).groupby('year').recordGross.sum()
    for y in sorted(ru.index):
        o[f'{y} stores / racks M'] = '%.1f / %.1f' % (ru.get(y, 0) / 1e6, rk.get(y, 0) / 1e6)
        o[f'{y} record gross $M'] = round(cm.get(y, 0) / 1e6)
    log = open(f'writing-calibration/{t}.log', encoding='utf-8', errors='ignore').read()
    o['bankruptcies'] = len(re.findall('Reason: Bankruptcy', log))
    return o


import numpy as np
runs = [a for a in sys.argv[1:] if not a.startswith('--')]
LAST = next((int(a[7:]) for a in sys.argv[1:] if a.startswith('--last=')), 104)
LAUNCH = next((tuple(int(x) for x in a[9:].split('-')) for a in sys.argv[1:] if a.startswith('--launch=')), (1, 52))
riaa = pd.Series(RIAA, index=LABELS); riaa = riaa / riaa.sum() * 100
pd.set_option('display.width', 250); pd.set_option('display.max_columns', 30)
if '--compact' in sys.argv:
    print(pd.DataFrame({t: compact(t) for t in runs}).to_string())
    sys.exit(0)
for t in runs:
    lp = cohort(t, LAST)
    print(f'\n===== {t}: {len(lp)} non-classical LPs launched weeks 1-52, units to week {LAST}')
    d = pd.DataFrame({'RIAA 1963': riaa.round(1), 'all store': dist(lp.units).round(1), 'all store+rack': dist(lp.storeRack).round(1),
                      'Major store+rack': dist(lp[lp.tier == 'Major'].storeRack).round(1),
                      'debut store+rack': dist(lp[lp.debut].storeRack).round(1),
                      'Major debut s+r': dist(lp[lp.debut & (lp.tier == 'Major')].storeRack).round(1)})
    print(d.to_string())
    rows = {}
    rows['all, store'] = summary(lp, 'units')
    rows['all, store+rack'] = summary(lp, 'storeRack')
    for tier in ('Major', 'MidTier', 'Boutique', 'Independent', 'Small'):
        rows[f'{tier}, s+r'] = summary(lp[lp.tier == tier], 'storeRack')
    rows['debut, s+r'] = summary(lp[lp.debut], 'storeRack')
    rows['non-debut, s+r'] = summary(lp[~lp.debut], 'storeRack')
    for tier in ('Major', 'MidTier', 'Independent', 'Small'):
        rows[f'debut {tier}, s+r'] = summary(lp[lp.debut & (lp.tier == tier)], 'storeRack')
    print(pd.DataFrame(rows).T.to_string())
    print('LPs per tier:', lp.tier.value_counts().to_dict(), '| debut share %.0f%%' % (100 * lp.debut.mean()))
