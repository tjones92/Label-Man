"""Audit fan calibration against history using complete weekly chart observations.

Usage: python SimTools/analyze_follower_calibration.py RUN [RUN ...]
Requires --chart-history and a completed audit. Outputs under SimLogs/follower-calibration.
Each title counts once at its first observed Top 40 week. Repeat means another
title by that act actually occupied the Top 40 in the prior 104 weeks. Opening
titles seed the lookback but are excluded from entry counts. No lifecycle dating,
eventual-peak lookahead, or omission of titles still active at the endpoint.
"""
import argparse
import hashlib
import json
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
LOGS = ROOT / 'SimLogs'


def entries(chart):
    top = chart[chart.currentPosition.between(1, 40)].copy()
    opening = set(chart.loc[chart.week.eq(0), 'recordId'])
    first = top.sort_values(['week', 'recordId']).drop_duplicates('recordId').copy()
    # A previous hit still charting qualifies, even if its first entry was >104 weeks ago.
    by_act = {a: g for a, g in top.groupby('artistId')}
    first['repeat'] = [bool(((by_act[r.artistId].week < r.week)
                            & (by_act[r.artistId].week >= r.week - 104)
                            & by_act[r.artistId].recordId.ne(r.recordId)).any())
                       for r in first.itertuples()]
    return first[~first.recordId.isin(opening)], top


def concentration(values, proportion):
    counts = values.sort_values(ascending=False)
    n = max(1, round(proportion * len(counts)))
    return float(counts.iloc[:n].sum() / counts.sum()) if counts.sum() else 0.


def summarize(chart, label):
    hits, top = entries(chart)
    annual = []
    for year, g in hits.groupby('year'):
        annual.append(dict(run=label, year=int(year), entries=len(g), acts=g.artistId.nunique(),
                           repeat=float(g.repeat.mean()),
                           top5=concentration(g.groupby('artistId').size(), .05)))
    # Match the length of the historical 1962-64 window, rather than comparing 3 to 4 years.
    # Ignore a short final boundary year (e.g. week 209 at the start of 1967).
    sizes = chart[chart.week.gt(0)].groupby('year').week.nunique()
    end_year = int(sizes[sizes.ge(45)].index.max())
    start_year = end_year - 2
    if any(sizes.get(year, 0) < 45 for year in range(start_year, end_year + 1)):
        raise ValueError(f'{label}: need three complete comparison years')
    window = hits[hits.year.between(start_year, end_year)]
    observed = chart[chart.week.gt(0) & chart.year.between(start_year, end_year)]
    top_window = observed[observed.currentPosition.le(40)]
    durations = top_window.groupby('recordId').size()
    summary = dict(run=label, startYear=start_year, endYear=end_year,
        entries=len(window), hitActs=window.artistId.nunique(),
        top5Entries=concentration(window.groupby('artistId').size(), .05),
        top10SlotWeeks=concentration(observed.groupby('artistId').size(), .10),
        top5Top40Weeks=concentration(top_window.groupby('artistId').size(), .05),
        observedTop40WeeksMean=float(durations.mean()),
        observedTop40WeeksMedian=float(durations.median()))
    return hits, annual, summary


def career_entries(hits, end_week, label):
    """Common observed outcome for history and model; this does not infer releases."""
    first = hits.sort_values('week').drop_duplicates('artistId')
    first = first[first.week.between(5, end_week - 65)]
    rows = []
    for h in first.itertuples():
        following = hits[hits.artistId.eq(h.artistId) & hits.week.between(h.week + 1, h.week + 52)]
        rows.append(dict(run=label, artistId=h.artistId, hitWeek=h.week,
            newTop40Entries52=len(following), noNewTop40Entry52=int(following.empty)))
    return pd.DataFrame(rows)


def funnel(chart, hits, launches, label):
    end = int(chart.week.max())
    first40 = chart[chart.currentPosition.le(40)].groupby('recordId').week.min()
    first100 = chart.groupby('recordId').week.min()
    cohort = hits.sort_values('week').drop_duplicates('artistId')
    # Every selected act has 52 weeks for releases and another 13 for chart outcomes.
    cohort = cohort[cohort.week.between(5, end - 65)]
    rows, followups = [], []
    for h in cohort.itertuples():
        q = launches[launches.artistId.eq(h.artistId) & launches.week.between(h.week + 1, h.week + 52)].copy()
        q['top40_13'] = q.singleId.map(first40).between(q.week, q.week + 13)
        q['top100_13'] = q.singleId.map(first100).between(q.week, q.week + 13)
        q['hitWeek'] = h.week
        q['run'] = label
        followups.append(q)
        rows.append(dict(run=label, artistId=h.artistId, hitWeek=h.week, releases52=len(q),
            noRelease=int(q.empty), top40Followups=int(q.top40_13.sum()),
            firstReleaseGap=None if q.empty else int(q.week.min() - h.week)))
    return pd.DataFrame(rows), pd.concat(followups, ignore_index=True) if followups else pd.DataFrame()


def book_outcomes(chart, hits, launches, label):
    f = pd.read_csv(LOGS / f'{label}-following.csv', low_memory=False)
    books = f[f.kind.eq('launch') & f.detail.str.startswith('Single', na=False)].copy()
    books['singleId'] = books.detail.str.rsplit('|', n=1).str[-1]
    books['book'] = books.detail.str.extract(r'book=([0-9]+)').astype(float)
    books = books[['singleId', 'book']].merge(launches, on='singleId', validate='one_to_one')
    first40 = hits.set_index('recordId').week
    # All launches get the same 13-week outcome window; do not call recent titles misses.
    books = books[books.week.le(chart.week.max() - 13)].copy()
    books['top40_13'] = books.singleId.map(first40).between(books.week, books.week + 13)
    books['run'] = label
    books['bookBand'] = pd.cut(books.book, [0, 10000, 30000, 100000, 300000, float('inf')]).astype(str)
    return books


def leader_releases(hits, chart, launches, books, summary, label):
    """Descriptive, retrospectively selected leaders; not a matched causal cohort."""
    window = hits[hits.year.between(summary['startYear'], summary['endYear'])]
    counts = window.groupby('artistId').size().sort_values(ascending=False)
    leaders = counts.head(max(1, round(.05 * len(counts)))).index
    q = launches[launches.artistId.isin(leaders) & launches.year.between(summary['startYear'], summary['endYear'])
                 & launches.week.le(chart.week.max() - 13)].copy()
    first40 = hits.set_index('recordId').week
    q['top40_13'] = q.singleId.map(first40).between(q.week, q.week + 13)
    q = q.merge(books[['singleId', 'book']], on='singleId', how='left', validate='one_to_one')
    q['run'] = label
    q['leaderActs'] = len(leaders)
    return q


def economic_pairs(pairs):
    rows = []
    for base, treatment in pairs:
        a = pd.read_csv(LOGS / f'{base}-decade-annual-rollup.csv').set_index('year')
        b = pd.read_csv(LOGS / f'{treatment}-decade-annual-rollup.csv').set_index('year')
        ca = pd.read_csv(LOGS / f'{base}-concentration.csv').set_index('year')
        cb = pd.read_csv(LOGS / f'{treatment}-concentration.csv').set_index('year')
        for year in sorted(set(a.index) & set(b.index) & {1963, 1964, 1965, 1966}):
            rows.append(dict(base=base, treatment=treatment, year=year,
                singlesPct=100 * (b.loc[year, 'singleUnits'] / a.loc[year, 'singleUnits'] - 1),
                albumsPct=100 * (b.loc[year, 'albumUnits'] / a.loc[year, 'albumUnits'] - 1),
                familySharePt=100 * (cb.loc[year, 'majorFamilyChartShare'] - ca.loc[year, 'majorFamilyChartShare']),
                cr4Pt=100 * (cb.loc[year, 'c4ChartShare'] - ca.loc[year, 'c4ChartShare'])))
    return pd.DataFrame(rows)


def history():
    p = LOGS / 'historical-hot100-1960-64/hot100_weekly.csv'
    c = pd.read_csv(p, parse_dates=['chartDate'])
    dates = sorted(c.chartDate.unique())
    c['week'] = c.chartDate.map({d: i for i, d in enumerate(dates)})
    c['year'] = c.chartDate.dt.year
    c['recordId'] = c.artist + ' || ' + c.title
    c['artistId'] = c.artist
    c['currentPosition'] = c.position
    return c


def load(run):
    console = (LOGS / f'{run}-console.log').read_text(encoding='utf-8', errors='replace')
    if f'CHART_AUDIT_COMPLETE run={run} ' not in console:
        raise ValueError(f'{run}: audit is not complete')
    c = pd.read_csv(LOGS / f'{run}-records.csv')
    c = c[c.currentPosition.between(1, 100)]
    if c.duplicated(['week', 'recordId']).any() or c.duplicated(['week', 'currentPosition']).any():
        raise ValueError(f'{run}: duplicate chart identities or positions')
    counts = c.groupby('week').size()
    if counts.min() != 100 or counts.max() != 100 or list(counts.index) != list(range(int(counts.index.max()) + 1)):
        raise ValueError(f'{run}: need opening chart and exactly 100 observations every week')
    return c


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('runs', nargs='*')
    ap.add_argument('--pair', nargs=2, action='append', default=[], metavar=('BASE', 'TREATMENT'))
    args = ap.parse_args()
    out = LOGS / 'follower-calibration'
    out.mkdir(exist_ok=True)
    annual, summary, cohorts, releases, books, careers, leaders = [], [], [], [], [], [], []
    for name in ['history'] + args.runs:
        c = history() if name == 'history' else load(name)
        h, a, s = summarize(c, name)
        annual.extend(a); summary.append(s)
        h.to_csv(out / f'{name}-entries.csv', index=False)
        careers.append(career_entries(h, int(c.week.max()), name))
        if name != 'history':
            launches = pd.read_csv(LOGS / f'{name}-single-launch-quality.csv')
            co, re = funnel(c, h, launches, name)
            cohorts.append(co); releases.append(re)
            bo = book_outcomes(c, h, launches, name)
            books.append(bo)
            leaders.append(leader_releases(h, c, launches, bo, s, name))
            print(name, 'first-hit cohort', len(co), 'no followup', co.noRelease.mean(),
                  'releases/act', co.releases52.mean(), 'Top40/followup', re.top40_13.mean(),
                  'Top100/followup', re.top100_13.mean(), 'first release gap', co.firstReleaseGap.median())
    pd.DataFrame(annual).to_csv(out / 'annual.csv', index=False)
    pd.DataFrame(summary).to_csv(out / 'summary.csv', index=False)
    ca = pd.concat(careers)
    ca.to_csv(out / 'career-entry-cohorts.csv', index=False)
    print(ca.groupby('run').agg(acts=('artistId', 'size'),
        entries52=('newTop40Entries52', 'mean'), noEntry52=('noNewTop40Entry52', 'mean')).to_string())
    if cohorts:
        pd.concat(cohorts).to_csv(out / 'cohorts.csv', index=False)
        pd.concat(releases).to_csv(out / 'followups.csv', index=False)
        b = pd.concat(books)
        b.to_csv(out / 'book-outcomes.csv', index=False)
        b.groupby(['run', 'year', 'bookBand']).agg(launches=('singleId', 'size'),
            top40_13=('top40_13', 'mean'), meanQuality=('runtimeQuality', 'mean')).to_csv(out / 'book-bands.csv')
        ld = pd.concat(leaders)
        ld.to_csv(out / 'leader-releases.csv', index=False)
        ld.groupby(['run', 'year']).agg(leaderActs=('leaderActs', 'first'), releases=('singleId', 'size'),
            top40_13=('top40_13', 'mean'), medianBook=('book', 'median')).to_csv(out / 'leader-years.csv')
    if args.pair:
        if any(run not in args.runs for pair in args.pair for run in pair):
            raise ValueError('Every pair source must also be supplied as a run')
        ep = economic_pairs(args.pair)
        ep.to_csv(out / 'economic-pairs.csv', index=False)
        print(ep.to_string(index=False))
        by_run = pd.DataFrame(summary).set_index('run')
        by_year = pd.DataFrame(annual).set_index(['run', 'year'])
        changes = []
        for base, treatment in args.pair:
            row = dict(base=base, treatment=treatment,
                repeat1966Pt=100 * (by_year.loc[(treatment, 1966), 'repeat'] - by_year.loc[(base, 1966), 'repeat']))
            for metric in ['top5Entries', 'top10SlotWeeks', 'top5Top40Weeks']:
                row[metric + 'Pt'] = 100 * (by_run.loc[treatment, metric] - by_run.loc[base, metric])
            row['observedTop40WeeksMeanChange'] = (by_run.loc[treatment, 'observedTop40WeeksMean']
                                                   - by_run.loc[base, 'observedTop40WeeksMean'])
            changes.append(row)
        pd.DataFrame(changes).to_csv(out / 'chart-pairs.csv', index=False)
    hashes = {run: {stream: hashlib.sha256((LOGS / f'{run}-{stream}').read_bytes()).hexdigest()
                    for stream in ['records.csv', 'single-launch-quality.csv', 'following.csv', 'console.log']}
              for run in args.runs}
    (out / 'sources.json').write_text(json.dumps(dict(runs=args.runs, pairs=args.pair, sha256=hashes,
        definition='first observed Top40 entry; prior actual Top40 occupancy by another title within 104 weeks; opening excluded',
        history='as-printed artist/title credits; no A/B-side merging; latest three complete years per source'), indent=2))
    print(pd.DataFrame(annual).to_string(index=False))
    print(pd.DataFrame(summary).to_string(index=False))


if __name__ == '__main__':
    main()
