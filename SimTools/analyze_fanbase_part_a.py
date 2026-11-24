"""Part A / part B reads of a finished fanbase decade run (FanbaseEfficacyEvidence.md). Read-only. For each run name:
Top 10 chart-week share by the act's first-chart cohort, Top 40 entries by cohort, the 1960-61 teen cohort's Top 10
share (acts whose first charted record was DooWop, RockAndRoll or TeenPop), the largest single week per year, the
monthly follower flows, and a watch on the late-arriving rock genres (folk rock, hard rock, proto-metal).
Usage: python SimTools/analyze_fanbase_part_a.py RUN [RUN ...]"""
import sys, collections
sys.path.insert(0, __import__('os').path.dirname(__import__('os').path.abspath(__file__)))
from veteran_demand_common import rows

OLD = ('DooWop', 'RockAndRoll', 'TeenPop')
WATCH = ('FolkRock', 'HardRock', 'ProtoMetal')

def cohort(fy):
    if fy is None: return None
    return '60-61' if fy <= 1961 else '62-63' if fy <= 1963 else '64-65' if fy <= 1965 else '66-67' if fy <= 1967 else '68-69'

def main(name):
    first = {}; fgen = {}; t10 = collections.Counter(); t40e = collections.Counter(); maxu = {}; peakrec = {}
    recs = []
    for r in rows(name, 'records'):
        w = int(r['week']); y = int(r['year']); p = int(r['currentPosition']); a = r['artistId'].strip('"'); rec = r['recordId'].strip('"')
        u = int(r['unitsThisWeek'])
        if u > maxu.get(y, (0,))[0]: maxu[y] = (u, r['title'].strip('"'), r['genre'].strip('"'))
        if 1 <= p <= 100:
            recs.append((w, y, p, a, rec))
            if a not in first or (w < first[a][0]): first[a] = (w, y); fgen[a] = r['genre'].strip('"')
    seen40 = set()
    for w, y, p, a, rec in sorted(recs):
        c = cohort(first[a][1])
        if p <= 10: t10[(y, c)] += 1
        if p <= 40 and rec not in seen40: seen40.add(rec); t40e[(y, c)] += 1
    teen = {a for a in first if first[a][1] <= 1961 and fgen[a] in OLD}
    cs = ['60-61', '62-63', '64-65', '66-67', '68-69']
    print(f'\n== {name}')
    print('Top 10 slot-week share by first-chart cohort | Top 40 entries by cohort | teen-cohort Top10 share | max single week')
    for y in range(1960, 1970):
        tot = sum(t10[(y, c)] for c in cs) or 1
        teen10 = sum(1 for w, yy, p, a, rec in recs if yy == y and p <= 10 and a in teen) / tot
        print(y, ' '.join(f'{c}:{t10[(y, c)] / tot:4.0%}' for c in cs), '|', ' '.join(f'{t40e[(y, c)]:3d}' for c in cs), f'| {teen10:4.0%} | {maxu[y][0] / 1e3:6.1f}k {maxu[y][1][:22]} ({maxu[y][2]})')
    # follower flows, teen cohort vs everyone, by year
    F = ['buy', 'tv', 'ageToAdult', 'agedLost', 'adultLoss', 'youngLoss', 'selfYoung', 'selfAdult']
    flows = collections.defaultdict(lambda: [0.0] * 8); stock = {}
    for r in rows(name, 'following'):
        if r['kind'] != 'act': continue
        y = int(r['date'].split('/')[-1]); a = r['artistId']; v = r['value'].split('|')
        if len(v) < 8: continue
        g = 'teen' if a in teen else 'all'
        for k in (g, 'all') if g == 'teen' else ('all',):
            f = flows[(y, k)]
            for i in range(8): f[i] += float(v[i])
        stock[(y, a)] = (float(r['young']), float(r['adult']))
    print('follower flows (M/yr): group year | buy tv selfY selfA | ageToAdult agedLost youngLoss adultLoss | year-end young adult')
    for k in ('teen', 'all'):
        for y in range(1960, 1970):
            f = flows[(y, k)]
            ys = sum(v[0] for (yy, a), v in stock.items() if yy == y and (k == 'all' or a in teen))
            ad = sum(v[1] for (yy, a), v in stock.items() if yy == y and (k == 'all' or a in teen))
            print(f'{k:4s} {y} | {f[0] / 1e6:6.2f} {f[1] / 1e6:5.2f} {f[6] / 1e6:5.2f} {f[7] / 1e6:5.2f} | {f[2] / 1e6:5.2f} {f[3] / 1e6:5.2f} {f[5] / 1e6:5.2f} {f[4] / 1e6:5.2f} | {ys / 1e6:6.2f} {ad / 1e6:6.2f}')


    # Genre watch: releases, market share, charting records, Top 40 / Top 10 record-weeks, and ratified moves into it.
    moves = collections.Counter()
    for r in rows(name, 'artist-evolution'):
        g = r['toGenre'].strip('"')
        if r['ratified'] == 'true' and g in WATCH: moves[(int(r['year']), g)] += 1
    print('genre watch: year genre | new releases  market share  charting records  Top40 wks  Top10 wks | ratified moves in')
    for r in rows(name, 'genre-decade-shape'):
        g = r['genre'].strip('"'); y = int(r['year'])
        if g in WATCH and 1964 <= y <= 1969:
            print(f"{y} {g:10s} | {r['newReleases']:>5s} {float(r['marketUnitsShare']):8.2%} {r['uniqueChartingRecords']:>5s} {r['top40RecordWeeks']:>6s} {r['top10RecordWeeks']:>6s} | {moves[(y, g)]:3d}")

if __name__ == '__main__':
    for n in sys.argv[1:]: main(n)
