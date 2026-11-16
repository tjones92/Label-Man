"""Known-weighted LP scale screen (GameyBehaviorAudit.md 2.12-2.13).

Run from SimLogs/: python ../SimTools/analyze_lp_known_scale.py fh-v1-2002-lref kn16-v1-2002-kn kn25-v1-2002-kn ...
LP cohort = LPs launched weeks 1-52 (--launch-weeks=N to change), lifetime units at the run's last snapshot. "Best single" maps acts through
demand-release-census (an act with no single counts as under 1k). Bankruptcies from SimLogs/writing-calibration/<run>.log.
"""
import pandas as pd, re, sys


def cohort(t):
    ro = pd.read_csv(f'{t}-release-outcomes.csv', usecols=['recordId', 'format', 'pressingBookSold', 'realizedNet'])
    lv = pd.read_csv(f'{t}-live-records-snapshot.csv'); lv = lv[lv.week == lv.week.max()]
    u = pd.concat([ro.rename(columns={'pressingBookSold': 'units', 'realizedNet': 'net'})[['recordId', 'format', 'units', 'net']],
                   lv.rename(columns={'totalUnitsSold': 'units', 'observedNetLowerBound': 'net'})[['recordId', 'format', 'units', 'net']]]
                  ).drop_duplicates('recordId', keep='last').set_index('recordId')
    api = pd.read_csv(f'{t}-demand-release-census.csv', usecols=['recordId', 'artistId', 'launchTier']).drop_duplicates('recordId').join(u, on='recordId', how='inner')
    dw = pd.read_csv(f'{t}-album-projects.csv', usecols=['albumRecordId', 'dropWeek']).dropna().drop_duplicates('albumRecordId').set_index('albumRecordId').dropWeek
    api['week'] = api.recordId.map(dw)
    best = api[api.format == 'Single'].groupby('artistId').units.max()
    lp = api[(api.format == 'Album') & (api.week <= LAUNCH_WEEKS)].copy()
    lp['best'] = lp.artistId.map(best).fillna(0)
    lp['band'] = pd.cut(lp.best, [-1, 999, 99999, 1e12], labels=['<1k', '1k-100k', '100k+'])
    return lp


def metrics(t):
    o = {}
    lp = cohort(t)
    for b, g in lp.groupby('band', observed=True):
        o[f'LP median, best single {b}'] = round(g.units.median() / 1e3, 1)
        o[f'LP units M, best single {b}'] = round(g.units.sum() / 1e6, 2)
    for tier in ('Major', 'MidTier', 'Independent', 'Small'):
        o[f'<1k median, {tier} (k)'] = round(lp[(lp.band == '<1k') & (lp.launchTier == tier)].units.median() / 1e3, 1)
    o['hit / unknown median'] = round(lp[lp.band == '100k+'].units.median() / lp[lp.band == '<1k'].units.median(), 2)
    o['LPs losing money %'] = round(100 * (lp.net < 0).mean(), 1)
    s = lp.units.sort_values(ascending=False)
    o['top 10% share of LP units'] = round(100 * s.head(len(s) // 10).sum() / s.sum(), 1)
    o['best LP, stores (k)'] = round(s.max() / 1e3)
    o['cohort LP units (M)'] = round(s.sum() / 1e6, 1)
    ru = pd.read_csv(f'{t}-decade-annual-rollup.csv', usecols=['year', 'albumUnits']).set_index('year').albumUnits
    rk = pd.read_csv(f'{t}-rack-title-weekly.csv', usecols=['year', 'units']).groupby('year').units.sum()
    bp = pd.read_csv(f'{t}-budget-product-weekly.csv', usecols=['year', 'units']).groupby('year').units.sum()
    cm = pd.read_csv(f'{t}-company-monthly.csv', usecols=['year', 'recordGross']).groupby('year').recordGross.sum()
    for y in sorted(ru.index):
        o[f'{y} stores / racks / budget (M)'] = '%.1f / %.1f / %.1f' % (ru.get(y, 0) / 1e6, rk.get(y, 0) / 1e6, bp.get(y, 0) / 1e6)
        o[f'{y} record gross ($M)'] = round(cm.get(y, 0) / 1e6)
    log = open(f'writing-calibration/{t}.log', encoding='utf-8', errors='ignore').read()
    o['bankruptcies'] = len(re.findall('Reason: Bankruptcy', log))
    return o


LAUNCH_WEEKS = next((int(a[15:]) for a in sys.argv[1:] if a.startswith('--launch-weeks=')), 52)
runs = [a for a in sys.argv[1:] if not a.startswith('--')]
r = pd.DataFrame({t: metrics(t) for t in runs})
pd.set_option('display.width', 250); pd.set_option('display.max_columns', 20)
print(r.to_string())
