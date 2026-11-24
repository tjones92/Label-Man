"""Shared loaders for the veteran demand-stack static replays (read-only)."""
import csv, gzip, collections, os, re
LOGS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'SimLogs')
def run_name(seed): return seed if isinstance(seed, str) else f'stature-marketing-decade-v2-{seed}-control'
def rows(seed, name):
    """seed is a decade-control seed or a full run name; exports may be gzip or plain."""
    path = os.path.join(LOGS, f'{run_name(seed)}-{name}.csv')
    opener = (lambda: gzip.open(path + '.gz', 'rt', newline='', encoding='utf-8')) if os.path.exists(path + '.gz') else (lambda: open(path, newline='', encoding='utf-8'))
    with opener() as f:
        yield from csv.DictReader(f)
def cohort_label(y):
    if y is None: return 'never'
    if y <= 1960: return '1960'
    if y <= 1962: return '1961-62'
    if y <= 1964: return '1963-64'
    if y <= 1966: return '1965-66'
    return '1967-69'
COHORTS = ['1960', '1961-62', '1963-64', '1965-66', '1967-69', 'never']
def load_chart(seed):
    """Hot 100 rows keyed by week, plus each act's first observed Hot 100 year."""
    weeks = collections.defaultdict(list); first = {}
    for r in rows(seed, 'records'):
        p = int(r['currentPosition'])
        if not 1 <= p <= 100: continue
        y = int(r['year']); a = r['artistId']
        if a not in first or y < first[a]: first[a] = y
        weeks[int(r['week'])].append(dict(rec=r['recordId'], artist=a, year=y, pos=p, units=int(r['unitsThisWeek']),
            points=float(r['chartPoints']), age=int(r['weeksSinceRelease']), tier=r['labelTier'], total=int(r['totalUnitsSold'])))
    return weeks, first
