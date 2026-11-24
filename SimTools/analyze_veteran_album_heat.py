"""Artist heat reaching 1964 singles at launch, split into single- and album-caused parts, per channel arm. Read-only."""
import sys, json, collections, statistics, bisect
from veteran_demand_common import *

def summarize(run, year=1964):
    weeks, first = load_chart(run)
    heat = collections.defaultdict(dict)
    for r in rows(run, 'artist-heat-attribution'):
        heat[r['artistId'].strip('"')][int(r['week'])] = (float(r['originalHeat']), float(r['singlesOnlyHeat']), float(r['currentAlbumsHeat']), int(r['albumHits']), int(r['singleHits']))
    res = {}
    for c in ('1960', '1961-62', '1963-64'):
        v = []
        for r in rows(run, 'single-launch-attribution'):
            if int(r['year']) != year: continue
            a = r['artistId'].strip('"')
            if cohort_label(first.get(a)) != c: continue
            w = int(r['week']); s = (w - 1) // 4 * 4 + 1   # the sample at or before launch
            v.append(heat.get(a, {}).get(s, (0.0, 0.0, 0.0, 0, 0)))
        n = len(v)
        res[c] = dict(launches=n, heat=statistics.fmean(x[0] for x in v), singles_only=statistics.fmean(x[1] for x in v),
            current_charts_only=statistics.fmean(x[2] for x in v), album_hits=statistics.fmean(x[3] for x in v), single_hits=statistics.fmean(x[4] for x in v),
            share_with_album_heat=sum(x[0] - x[1] > 1e-6 for x in v) / n)
    return res

if __name__ == '__main__':
    runs = sys.argv[1:] or [f'veteran-follower-channels-261w-v2-2002-{a}' for a in ('full', 'no-stock', 'no-intent')]
    out = {}
    for run in runs:
        run = int(run) if run.isdigit() else run
        years = (1964,) if isinstance(run, str) else (1964, 1968)
        for y in years:
            out[f'{run}|{y}'] = r = summarize(run, y)
            print(f'\n{run} {y}: artist heat read by single launches')
            for c, d in r.items():
                print(f"  {c:8s} launches {d['launches']:4d} heat {d['heat']:.3f} = singles {d['singles_only']:.3f} + albums {d['heat']-d['singles_only']:.3f}"
                      f" (retained album peaks {d['heat']-d['current_charts_only']:.3f}) | active album hits {d['album_hits']:.2f} single hits {d['single_hits']:.2f} | launches with album heat {d['share_with_album_heat']:.0%}")
    json.dump(out, open(os.path.join(LOGS, 'veteran-demand', 'album-heat-v1.json'), 'w'), indent=1)
