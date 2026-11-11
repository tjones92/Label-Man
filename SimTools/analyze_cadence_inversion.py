"""Cadence-inversion screen readout (run_cadence_inversion_screen.ps1). Read-only.
Per run and year: Major Top 10 slot-week share, single launches by tier, singles per Established act-year by tier
(acts signed >= 26 weeks, act-release-years), Top 10 weeks per Established launch by tier, the adult 1960-61 cohort's
Top 10 share and launches, and units sold by logged records (records.csv).
Usage: py SimTools/analyze_cadence_inversion.py RUN [RUN ...]"""
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from veteran_demand_common import rows

TEEN = ('DooWop', 'RockAndRoll', 'TeenPop')
G = ('Maj', 'Mid', 'Ind')
def grp(tier): return 'Maj' if tier == 'Major' else 'Mid' if tier == 'MidTier' else 'Ind'

def main(run):
    units = collections.Counter(); best = {}; sw = collections.Counter(); t10 = collections.Counter(); first = {}; fgen = {}; tier_of = {}
    for r in rows(run, 'records'):
        p = int(r['currentPosition']); rec = r['recordId']; a = r['artistId']; w = int(r['week']); y = int(r['year'])
        tier_of.setdefault(rec, r['labelTier']); units[y] += int(r['unitsThisWeek'])
        if not 1 <= p <= 100: continue
        best[rec] = min(best.get(rec, 999), p)
        if a not in first or w < first[a][0]: first[a] = (w, y); fgen[a] = r['genre']
        if p <= 10: sw[rec] += 1; t10[(y, grp(tier_of[rec]))] += 1; t10[(y, a)] += 1; t10[y] += 1
    adult = {a for a in first if first[a][1] <= 1961 and fgen[a] not in TEEN}
    launches = collections.Counter(); est = collections.defaultdict(lambda: [0, 0]); adultL = collections.Counter()
    for r in rows(run, 'single-launch-quality'):
        y = int(r['year']); g = grp(r['tier']); launches[(y, g)] += 1
        if r['artistId'] in adult: adultL[y] += 1
        if r['careerState'] == 'Established': est[(y, g)][0] += 1; est[(y, g)][1] += sw[r['singleId']]
    ay = collections.defaultdict(lambda: [0, 0])
    for r in rows(run, 'act-release-years'):
        if int(r['signedWeeks']) >= 26 and r['careerStateAtStart'] == 'Established':
            k = (int(r['year']), grp(r['tier'])); ay[k][0] += 1; ay[k][1] += int(r['singles'])
    years = sorted({y for (y, g) in launches})
    print(f'\n== {run}')
    print('year | Maj T10 | launches Maj/Mid/Ind | Est singles/act-yr Maj/Mid/Ind | Est T10wk/launch Maj/Mid/Ind | adult60 T10 share, launches | units M')
    for y in years:
        T = t10[y] or 1
        a60 = sum(v for k, v in t10.items() if isinstance(k, tuple) and k[0] == y and k[1] in adult)
        print(f"{y} | {t10[(y, 'Maj')] / T:5.1%} | " + '/'.join(f'{launches[(y, g)]:4d}' for g in G) + ' | ' +
              '/'.join(f'{ay[(y, g)][1] / max(1, ay[(y, g)][0]):.2f}' for g in G) + ' | ' +
              '/'.join(f'{est[(y, g)][1] / max(1, est[(y, g)][0]):.2f}' for g in G) +
              f' | {a60 / T:5.1%} {adultL[y]:4d} | {units[y] / 1e6:6.1f}')

if __name__ == '__main__':
    for run in sys.argv[1:]: main(run)
