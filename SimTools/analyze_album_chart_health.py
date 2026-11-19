"""LP chart health against Billboard (LpGenreAllocationFollowup.md §23).

Run from SimLogs/: python ../SimTools/analyze_album_chart_health.py tm-s09-2002-sc tm-s09-1001-sc ...
Needs *-album-chart.csv (--album-chart-history, or a run without --calibration).

Per calendar year (years with fewer than 40 chart weeks are skipped), per run, then the mean across runs:
  size        chart size in the year's last week (model: 40 to Apr 1961, 50 to Aug 1963, 150, 200 from May 1967)
  titles      distinct titles on the chart
  entries/wk  new titles per week (first chart week in the year; the run's first week is excluded)
  top10       distinct titles reaching the top 10
  no1         distinct No. 1 titles            (Billboard: 10 / 12 / ... / 9 in 1965 / 12 in 1968)
  maxNo1      longest consecutive No. 1 run    (Billboard: 13 in 1960, 10+ in 1961, 13 in 1965)
  t10wk       median weeks in the top 10 per top-10 title (all its weeks, any year)
  debutT10%   share of top-10 titles that reach the top 10 by their 2nd chart week (launch-spike entries)
  run         median total chart weeks of titles debuting in the year's first half (later chart weeks counted)
  old40%      share of chart slots held by titles 40+ weeks on the chart
              (Billboard, January 1960: 25 of 65 mono listings were 40+ week titles = 38%; that is why the
              Essential Inventory chart was split off)
  old20%      share of chart slots held by titles 20+ weeks on the chart (stereo Essential Inventory bar)
  debutPct    median debut position as % of chart size
              (Billboard 21 Apr and 5 May 1962, mono Top LPs of 150: 5 new titles each week, debuting at
              #105-148, i.e. 70-99%; stereo Top LPs of 50: 4 and 2 new titles, #36-50)
  debutT10n   debuts inside the top 10 in the year (count)
  climb       median chart week (1 = debut) of a top-10 title's peak
  st%T10      soundtrack share of top-10 titles
  #1/#10      median ratio of No. 1 to No. 10 weekly units
"""
import os, sys
import pandas as pd

BILLBOARD = {
    # year: (mono chart size at year end, distinct No. 1s, longest No. 1 run)
    1960: ('40 action + 25 EI', 10, 13),
    1961: ('150 (from ~Apr)', 12, '10+'),
    1962: ('150', None, None),
    1963: ('150 merged (Aug)', None, None),
    1965: ('150', 9, 13),
    1967: ('175 (Apr) -> 200 (May)', None, None),
    1968: ('200', 12, 9),
}


def longest_run(ids):
    best = cur = 0
    prev = None
    for i in ids:
        cur = cur + 1 if i == prev else 1
        prev = i
        best = max(best, cur)
    return best


def health(tag):
    a = pd.read_csv(next(p for p in (f'{tag}-album-chart.csv', f'{tag}-album-chart.csv.gz') if os.path.exists(p) or p.endswith('.gz')), usecols=['week', 'year', 'chartSize', 'position', 'recordId',
                                                       'albumFormat', 'unitsThisWeek', 'weeksOnChart'])
    first = a.groupby('recordId').week.min()
    a['firstWeek'] = a.recordId.map(first)
    a['chartWeekIdx'] = a.week - a.firstWeek + 1
    total = a.groupby('recordId').size()
    t10weeks = a[a.position <= 10].groupby('recordId').size()
    firstT10 = a[a.position <= 10].groupby('recordId').chartWeekIdx.min()
    peakAt = a.sort_values(['position', 'week']).drop_duplicates('recordId').set_index('recordId').chartWeekIdx
    rows = []
    for y, x in a.groupby('year'):
        weeks = sorted(x.week.unique())
        if len(weeks) < 40:
            continue
        t10 = x[x.position <= 10].recordId.unique()
        no1 = x[x.position == 1].sort_values('week').recordId
        newx = x[(x.week == x.firstWeek) & (x.week > a.week.min())]
        firsthalf = x[(x.week == x.firstWeek) & (x.week < weeks[0] + 26) & (x.week > a.week.min())].recordId.unique()
        p1 = x[x.position == 1].set_index('week').unitsThisWeek
        p10 = x[x.position == 10].set_index('week').unitsThisWeek
        fmt = x.drop_duplicates('recordId').set_index('recordId').albumFormat
        rows.append({
            'tag': tag, 'year': y, 'size': int(x[x.week == weeks[-1]].chartSize.max()),
            'titles': x.recordId.nunique(),
            'entries/wk': round(newx.recordId.nunique() / len(weeks), 1),
            'top10': len(t10), 'no1': no1.nunique(), 'maxNo1': longest_run(list(no1)),
            't10wk': float(t10weeks.reindex(t10).median()),
            'debutT10%': round(100 * (firstT10.reindex(t10) <= 2).mean()),
            'run': float(total.reindex(firsthalf).median()) if len(firsthalf) else float('nan'),
            'old40%': round(100 * (x.weeksOnChart >= 40).mean()),
            'old20%': round(100 * (x.weeksOnChart >= 20).mean()),
            'debutPct': round(100 * float((newx.position / newx.chartSize).median())),
            'debutT10n': int((newx.position <= 10).sum()),
            'climb': float(peakAt.reindex(t10).median()),
            'st%T10': round(100 * (fmt.reindex(t10) == 'Soundtrack').mean()),
            '#1/#10': round(float((p1 / p10).median()), 1),
        })
    return rows


if __name__ == '__main__':
    out = []
    for t in sys.argv[1:]:
        out += health(t)
    df = pd.DataFrame(out)
    cols = ['tag', 'year', 'size', 'titles', 'entries/wk', 'top10', 'no1', 'maxNo1', 't10wk', 'debutT10%', 'run',
            'old40%', 'old20%', 'debutPct', 'debutT10n', 'climb', 'st%T10', '#1/#10']
    pd.set_option('display.width', 200)
    print(df[cols].to_string(index=False))
    print()
    print('Mean across runs:')
    print(df[cols[1:]].groupby('year').mean().round(1).to_string())
    print()
    print('Billboard (mono size; distinct No. 1s; longest No. 1 run):')
    for y, v in BILLBOARD.items():
        print(' ', y, v)
