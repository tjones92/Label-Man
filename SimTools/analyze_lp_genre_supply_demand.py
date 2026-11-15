"""LP genre supply against demand per title (LP channel handoff issue 1, 2026-11-15).

Run from SimLogs/: python ../SimTools/analyze_lp_genre_supply_demand.py cand-v3-2002-off bud-v2-2002-lwbr ...
Universe: demand-release-census LP launches (opening-catalogue LPs are listed apart). Store units are lifetime
units at run end (release-outcomes pressingBookSold for retired titles, live-records-snapshot for live ones).
Per launch year and genre: share of titles, share of store units, units per title against the year's mean,
the genre's share of the year's 50 biggest LPs (the album-head proxy), and the format decision's album choice
probability and album affinity.
"""
import pandas as pd, sys
G = ['Folk', 'TraditionalPop', 'EasyListening', 'Classical', 'Jazz', 'Comedy', 'Soul', 'Country', 'BossaNova',
     'TeenPop', 'RockAndRoll']
YEARS = [int(a[8:]) for a in sys.argv[1:] if a.startswith('--years=')] or None
for t in [a for a in sys.argv[1:] if not a.startswith('--')]:
    c = pd.read_csv(f'{t}-demand-release-census.csv',
                    usecols=['recordId', 'artistId', 'labelId', 'format', 'genre', 'observationKind', 'observedAuditWeek',
                             'launchTier', 'runtimeQualityAtLaunch'])
    c = c[c.format == 'Album'].drop_duplicates('recordId').set_index('recordId')
    ro = pd.read_csv(f'{t}-release-outcomes.csv', usecols=['recordId', 'format', 'pressingBookSold'])
    lv = pd.read_csv(f'{t}-live-records-snapshot.csv'); lv = lv[lv.week == lv.week.max()]
    u = pd.concat([ro[ro.format == 'Album'].set_index('recordId').pressingBookSold,
                   lv[lv.format == 'Album'].set_index('recordId').totalUnitsSold])
    u = u[~u.index.duplicated(keep='last')]
    rk = pd.read_csv(f'{t}-rack-title-weekly.csv', usecols=['recordId', 'units']).groupby('recordId').units.sum()
    a = c.join(u.rename('store')).join(rk.rename('rack')).fillna({'store': 0, 'rack': 0})
    a['launch'] = (a.observedAuditWeek - 1) // 52 + 1960
    L = a[a.observationKind == 'Launch']
    fd = pd.read_csv(f'{t}-format-decision-explanation.csv',
                     usecols=['year', 'recordId', 'genre', 'chosenFormat', 'albumAffinity', 'albumChoiceProbability',
                              'finalSingleMargin', 'finalAlbumMargin'])
    print(f'\n=== {t}: LP launches {len(L)}, opening LPs {(a.observationKind == "Opening").sum()}')
    op = a[a.observationKind == 'Opening']
    print('opening LPs: title % / store-unit % by genre:',
          {g: (round(100 * (op.genre == g).mean(), 1), round(100 * op[op.genre == g].store.sum() / max(1, op.store.sum()), 1))
           for g in G[:8]})
    for y in YEARS or sorted(L.launch.unique()):
        s = L[L.launch == y]
        if len(s) < 100: continue
        top = s.sort_values('store', ascending=False).head(50)
        f = fd[fd.year == y]
        rows = {}
        for g in G:
            x = s[s.genre == g]; fx = f[f.genre == g]
            if not len(x): continue
            rows[g] = {
                'titles %': 100 * len(x) / len(s), 'units %': 100 * x.store.sum() / s.store.sum(),
                'units/title x': (x.store.mean() / s.store.mean()), 'median k': x.store.median() / 1e3,
                'top50 %': 100 * (top.genre == g).mean(), 'rack %': 100 * x.rack.sum() / max(1, s.rack.sum()),
                'acts': x.artistId.nunique(), 'LPs/act': len(x) / max(1, x.artistId.nunique()),
                'quality': x.runtimeQualityAtLaunch.mean(),
                'decisions %': 100 * len(fx) / max(1, len(f)), 'album p': fx.albumChoiceProbability.mean(),
                'chose LP %': 100 * (fx.chosenFormat == 'Album').mean(), 'affinity': fx.albumAffinity.mean(),
                'Major title %': 100 * (x.launchTier == 'Major').mean(),
                'Major unit %': 100 * x[x.launchTier == 'Major'].store.sum() / max(1, x.store.sum()),
            }
        print(f'-- {y} launches: {len(s)} LPs, {s.store.sum() / 1e6:.1f}M store units, mean {s.store.mean() / 1e3:.1f}k')
        print(pd.DataFrame(rows).T.round(2).to_string())
