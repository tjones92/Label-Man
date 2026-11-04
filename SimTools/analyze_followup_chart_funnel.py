"""Follow-up release -> Hot 100 -> Top 10 investigation on completed worlds.

Does not change the simulation. Uses common completed seeds across the three
mbk arms; never reads a running world's partial CSVs. Generated files belong in
SimLogs. Gap medians are conditional on the event, not a release-frequency target.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_hit_act_cadence import annual_metrics
from analyze_head_hit_conversion import require_complete_chart
from analyze_midtier_breakout import followup_cohorts
from analyze_midtier_breakout import validate_manifests
from analyze_offchart_screen import ROOT, path
from analyze_studio_parent import BIG5, HISTORY_BIG5, HISTORY_STUDIO, studio_ids


def event_starts(debuts, gap=3):
    """Merge chained same-act debuts <=3 weeks apart, matching the prior history audit.

    These are charting-event proxies, not verified A/B-side identities or release dates.
    """
    ordered = debuts.sort_values(['artistId', 'week', 'recordId']).copy()
    delta = ordered.groupby('artistId').week.diff()
    return ordered[delta.isna() | delta.gt(gap)]


def first_hits(records, opening, end_week):
    hits = records[records.currentPosition.between(1, 10) & ~records.recordId.isin(opening)]
    hits = hits.sort_values(['week', 'currentPosition', 'recordId']).drop_duplicates('artistId')
    return hits[hits.week.between(5, end_week - 52)]


def funnel_rows(records, launches, opening, end_week):
    hits = first_hits(records, opening, end_week)
    chart = records[records.currentPosition.between(1, 100)]
    debuts = chart.sort_values(['week', 'recordId']).drop_duplicates('recordId')
    debut_week = debuts.set_index('recordId').week
    first10 = chart[chart.currentPosition.le(10)].groupby('recordId').week.min()
    events = event_starts(debuts[~debuts.recordId.isin(opening)])
    launched = {a: q for a, q in launches.groupby('artistId')}
    entered = {a: q for a, q in debuts.groupby('artistId')}
    chart_by_act_week = chart.groupby(['artistId', 'week']).recordId.agg(set).to_dict()
    cohorts, release_rows = [], []
    for hit in hits.itertuples():
        q = launched.get(hit.artistId, launches.iloc[:0])
        q = q[q.week.between(hit.week + 1, hit.week + 52)].sort_values(['week', 'singleId'])
        d = entered.get(hit.artistId, debuts.iloc[:0])
        d = d[d.week.between(hit.week + 1, hit.week + 52) & d.recordId.ne(hit.recordId) & ~d.recordId.isin(opening)]
        e = events[(events.artistId == hit.artistId) & events.week.between(hit.week + 1, hit.week + 52) & events.recordId.ne(hit.recordId)]
        successes = q[q.singleId.map(debut_week).between(hit.week + 1, hit.week + 52)]
        row = dict(artistId=hit.artistId, group=hit.group, hitWeek=int(hit.week), hitRecord=hit.recordId,
                   hitQuality=float(hit.quality), releases52=len(q), debuts52=len(d), eventDebuts52=len(e),
                   noRelease52=int(q.empty), noDebut52=int(d.empty),
                   firstReleaseGap=None if q.empty else int(q.week.iloc[0] - hit.week),
                   firstDebutGap=None if d.empty else int(d.week.min() - hit.week),
                   firstSuccessReleaseOrder=None if successes.empty else int(list(q.singleId).index(successes.singleId.iloc[0]) + 1),
                   nextDebutWasReleasedBeforeHit=int(not d.empty and d.sort_values('week').recordId.iloc[0] not in set(q.singleId)),
                   top10Debuts52=int(d.recordId.map(first10).between(hit.week + 1, hit.week + 52).sum()),
                   hitReleaseToTop10=int(hit.weeksSinceRelease),
                   subsequentDebutGap=list(d.sort_values('week').week.diff().dropna()),
                   releaseGaps=list(q.weeksSinceLastSingle[q.weeksSinceLastSingle.ge(0)]))
        cohorts.append(row)
        for order, launch in enumerate(q.itertuples(), 1):
            dw = debut_week.get(launch.singleId)
            tw = first10.get(launch.singleId)
            old = chart_by_act_week.get((hit.artistId, launch.week - 1), set()) - {launch.singleId}
            release_rows.append(dict(artistId=hit.artistId, group=hit.group, hitWeek=int(hit.week),
                singleId=launch.singleId, week=int(launch.week), order=order, hitGap=int(launch.week - hit.week),
                releaseGap=int(launch.weeksSinceLastSingle), launchTier=launch.tier, labelId=launch.labelId,
                strategy=launch.strategy, genre=launch.genre, careerState=launch.careerState,
                quality=float(launch.runtimeQuality), hook=float(launch.hook), production=float(launch.production),
                predecessors=len(old), originalHitOnChart=hit.recordId in old,
                entered52=int(pd.notna(dw) and dw <= hit.week + 52),
                top10In52=int(pd.notna(tw) and tw <= hit.week + 52),
                entryDelay=None if pd.isna(dw) else int(dw - launch.week),
                entry13Eligible=int(launch.week + 13 <= hit.week + 52),
                entered13=int(pd.notna(dw) and launch.week <= dw <= launch.week + 13),
                top10_13=int(pd.notna(tw) and launch.week <= tw <= launch.week + 13)))
    return pd.DataFrame(cohorts), pd.DataFrame(release_rows)


def historical():
    folder = Path(ROOT) / 'historical-hot100-1960-64'
    chart = pd.read_csv(folder / 'hot100_weekly.csv', parse_dates=['chartDate'])
    dates = sorted(chart.chartDate.unique())
    chart['week'] = chart.chartDate.map({date: n for n, date in enumerate(dates)})
    chart['artistId'] = chart.artist
    chart['recordId'] = chart.artist + ' || ' + chart.title
    chart['currentPosition'] = chart.position
    chart['quality'] = float('nan')
    chart['weeksSinceRelease'] = 0  # unknown: never interpret as an observed release age
    owners = pd.concat([pd.read_csv(Path(ROOT) / f'historical-top10-{year}' / f'records_{year}_grouped.csv')
                        for year in range(1960, 1965)])
    owners['recordId'] = owners.artist + ' || ' + owners.title
    owners = owners.sort_values('first').drop_duplicates('recordId').set_index('recordId').owned
    chart['group'] = chart.recordId.map(owners).map(lambda x: 'big5' if x in HISTORY_BIG5 else 'studio' if x in HISTORY_STUDIO else 'field' if pd.notna(x) else 'unknown')
    opening = set(chart.loc[chart.week == chart.week.min(), 'recordId'])
    hits = first_hits(chart, opening, int(chart.week.max()))
    debuts = chart.sort_values(['week', 'recordId']).drop_duplicates('recordId')
    first10 = chart[chart.position.le(10)].groupby('recordId').week.min()
    events = event_starts(debuts[~debuts.recordId.isin(opening)])
    rows = []
    for hit in hits.itertuples():
        d = debuts[(debuts.artistId == hit.artistId) & debuts.week.between(hit.week + 1, hit.week + 52) & debuts.recordId.ne(hit.recordId) & ~debuts.recordId.isin(opening)]
        e = events[(events.artistId == hit.artistId) & events.week.between(hit.week + 1, hit.week + 52) & events.recordId.ne(hit.recordId)]
        rows.append(dict(artistId=hit.artistId, group=hit.group, hitWeek=hit.week, debuts52=len(d), eventDebuts52=len(e),
                         noDebut52=int(d.empty), firstDebutGap=None if d.empty else int(d.week.min() - hit.week),
                         top10Debuts52=int(d.recordId.map(first10).between(hit.week + 1, hit.week + 52).sum()),
                         subsequentDebutGap=list(d.sort_values('week').week.diff().dropna())))
    return pd.DataFrame(rows)


def summary(cohorts, releases=None):
    result = dict(acts=len(cohorts), debutsPerAct=float(cohorts.debuts52.mean()),
                  chartEventsPerAct=float(cohorts.eventDebuts52.mean()),
                  noDebutShare=float(cohorts.noDebut52.mean()), firstDebutGap=float(cohorts.firstDebutGap.median()),
                  top10PerAct=float(cohorts.top10Debuts52.mean()),
                  top10PerEntry=float(cohorts.top10Debuts52.sum() / max(1, cohorts.debuts52.sum())),
                  subsequentDebutGap=float(pd.Series([v for row in cohorts.subsequentDebutGap for v in row], dtype=float).median()))
    if releases is not None:
        q = releases[releases.entry13Eligible.eq(1)]
        first = releases[releases.order.eq(1)]
        result.update(releasesPerAct=float(cohorts.releases52.mean()), noReleaseShare=float(cohorts.noRelease52.mean()),
                      firstReleaseGap=float(cohorts.firstReleaseGap.median()),
                      singleToSingleGap=float(releases.releaseGap[releases.releaseGap.ge(0)].median()),
                      hitReleaseToTop10=float(cohorts.hitReleaseToTop10.median()),
                      followupLaunches=len(releases), chartedFollowups=int(releases.entered52.sum()),
                      firstFollowupEntryShare=float(first.entered52.mean()),
                      entry13Launches=len(q), entry13Share=float(q.entered13.mean()),
                      entryDelayConditional=float(releases.loc[releases.entered52.eq(1), 'entryDelay'].median()),
                      followupQuality=float(releases.quality.mean()), firstHitQuality=float(cohorts.hitQuality.mean()),
                      overlapShare=float(releases.predecessors.gt(0).mean()))
    return result


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--tag', default='mbk-v1')
    ap.add_argument('--output', default='SimLogs/followup-chart-investigation')
    args = ap.parse_args()
    plan = json.loads((Path(ROOT) / args.tag / 'plan.json').read_text(encoding='utf-8-sig'))
    arms = dict(baseline=plan['baseline'], **plan['arms'])
    manifests = {arm: json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig')) for arm, tag in arms.items()}
    seeds = sorted(set.intersection(*[{r['seed'] for r in m['results'] if r['passed']} for m in manifests.values()]))
    if not seeds:
        raise ValueError('No common completed seeds')
    if seeds == sorted(plan['seeds']):
        validate_manifests(plan)
    out = Path(args.output)
    out.mkdir(parents=True, exist_ok=True)
    all_c, all_r, all_a = [], [], []
    for arm, tag in arms.items():
        manifest = manifests[arm]
        if manifest['weeks'] != plan['weeks']:
            raise ValueError('Duration mismatch')
        for seed in seeds:
            p = lambda stream: path(tag, seed, stream, plan['weeks'])
            records = pd.read_csv(p('records'))
            require_complete_chart(records[records.currentPosition.between(1, 100)], plan['weeks'])
            launches = pd.read_csv(p('single-launch-quality'))
            if launches.singleId.duplicated().any():
                raise ValueError('Duplicate launch')
            census = pd.read_csv(p('demand-release-census'))
            opening = set(census.loc[census.observationKind.eq('Opening'), 'recordId'])
            studios = studio_ids(p)
            records['group'] = records.labelId.map(lambda x: 'big5' if x in BIG5 else 'studio' if x in studios else 'field')
            c, r = funnel_rows(records, launches, opening, plan['weeks'])
            # Reconcile the published release/debut medians and cohort counts.
            old = pd.DataFrame(followup_cohorts(records, launches, opening, plan['weeks'])).set_index('artistId')
            check = c.set_index('artistId')
            for column in ['releases52', 'firstReleaseGap', 'firstDebutGap']:
                pd.testing.assert_series_equal(old[column].sort_index(), check[column].sort_index(), check_names=False, check_dtype=False)
            all_c.append(c.assign(arm=arm, seed=seed)); all_r.append(r.assign(arm=arm, seed=seed))
            all_a.extend(dict(arm=arm, seed=seed, year=year, **v) for year, v in annual_metrics(records, launches, opening).items())
    c, r, a = pd.concat(all_c), pd.concat(all_r), pd.DataFrame(all_a)
    c.to_json(out / 'cohorts.json', orient='records', indent=2)
    r.to_csv(out / 'followup-releases.csv', index=False)
    a.to_csv(out / 'annual.csv', index=False)
    hist = historical()
    hist.to_json(out / 'history-cohorts.json', orient='records', indent=2)
    summaries = []
    for (arm, seed, group), rows in c.groupby(['arm', 'seed', 'group']):
        releases = r[(r.arm == arm) & (r.seed == seed) & (r.group == group)]
        summaries.append(dict(arm=arm, seed=int(seed), group=group, **summary(rows, releases)))
    s = pd.DataFrame(summaries)
    s.to_csv(out / 'summary.csv', index=False)
    strata = []
    for column in ['order', 'careerState', 'strategy', 'genre', 'launchTier', 'predecessors']:
        for (arm, group, value), rows in r[r.entry13Eligible.eq(1)].groupby(['arm', 'group', column]):
            strata.append(dict(arm=arm, group=group, axis=column, value=str(value), launches=len(rows),
                               entry13Share=float(rows.entered13.mean()), top10_13Share=float(rows.top10_13.mean()),
                               quality=float(rows.quality.mean()), releaseGap=float(rows.releaseGap.median())))
    r['qualityBand'] = pd.cut(r.quality, [0, .6, .65, .7, .75, .8, 1.01], include_lowest=True).astype(str)
    for (arm, group, band), rows in r[r.entry13Eligible.eq(1)].groupby(['arm', 'group', 'qualityBand']):
        strata.append(dict(arm=arm, group=group, axis='qualityBand', value=band, launches=len(rows),
                           entry13Share=float(rows.entered13.mean()), top10_13Share=float(rows.top10_13.mean()), quality=float(rows.quality.mean())))
    pd.DataFrame(strata).to_csv(out / 'strata.csv', index=False)
    metadata = dict(plan=args.tag, weeks=plan['weeks'], commonCompletedSeeds=seeds, allSeedsComplete=seeds == sorted(plan['seeds']),
                    arms=arms, historyAll=summary(hist), historyField=summary(hist[hist.group.eq('field')]),
                    caution='Observational strata are not causal effects. Published compact CSVs omit off-chart trajectories.')
    (out / 'metadata.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print(json.dumps(metadata, indent=2))
    print(s[s.group.eq('field')].round(3).to_string(index=False))
    print('\nAnnual owner-group repeat means:')
    print(a.groupby('arm')[['repeat2', 'repeat3', 'field_repeat2', 'big5_repeat2', 'studio_repeat2']].mean().round(3).to_string())


if __name__ == '__main__':
    main()
