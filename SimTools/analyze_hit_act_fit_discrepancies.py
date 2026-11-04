"""Describe chart tradeoffs in a completed fit-floor screen without selecting a dose.

First run analyze_hit_act_fit_support.py with off, f06 and f1 arms. This reads
its validated, fixed-window outputs and the recovered historical charts. No
simulation reranking or individual-artist treatment effect is inferred.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_followup_chart_funnel import historical, summary
from analyze_hit_act_cadence import annual_metrics
from analyze_offchart_screen import ROOT
from analyze_studio_parent import HISTORY_BIG5, HISTORY_STUDIO


CHART_KEYS = [
    'repeat2', 'repeat3', 'field_repeat2', 'big5_repeat2', 'studio_repeat2',
    'hot100Records', 'top10Records', 'top10WeeksPerRecord', 'labelsWithTop10',
    'top10SlotHHI', 'top1LabelSlotShare', 'numberOneRecords', 'maxNumberOneStreak',
    'big5_top10Share', 'studio_top10Share', 'field_top10Share',
]


def historical_chart(years):
    chart = pd.read_csv(Path(ROOT) / 'historical-hot100-1960-64' / 'hot100_weekly.csv', parse_dates=['chartDate'])
    dates = sorted(chart.chartDate.unique())
    chart['week'] = chart.chartDate.map({d: n for n, d in enumerate(dates)})
    chart['year'] = chart.chartDate.dt.year
    owners = pd.concat([pd.read_csv(Path(ROOT) / f'historical-top10-{y}' / f'records_{y}_grouped.csv').assign(year=y)
                        for y in years])
    chart = chart.merge(owners[['year', 'artist', 'title', 'owned']], on=['year', 'artist', 'title'], how='left', validate='many_to_one')
    missing = chart[chart.year.isin(years) & chart.position.le(10) & chart.owned.isna()]
    if len(missing):
        raise ValueError(f'Historical Top 10 owner missing: {missing[["artist", "title"]].drop_duplicates().to_dict("records")}')
    chart['artistId'] = chart.artist
    chart['recordId'] = chart.artist + ' || ' + chart.title
    chart['currentPosition'] = chart.position
    chart['labelId'] = chart.owned
    chart['group'] = chart.owned.map(lambda x: 'big5' if x in HISTORY_BIG5 else 'studio' if x in HISTORY_STUDIO else 'field' if pd.notna(x) else 'unknown')
    # The release-based metrics are intentionally discarded: history has chart dates only.
    launches = pd.DataFrame(columns=['year', 'artistId', 'weeksSinceLastSingle'])
    result = annual_metrics(chart, launches, set(), years=years)
    return pd.DataFrame([dict(year=y, **{k: m[k] for k in CHART_KEYS}) for y, m in result.items()])


def rank_bands(releases):
    """Retain misses and use the same eligible launch window in every arm."""
    q = releases[releases.entry13Eligible.eq(1)].copy()
    if q.peakIn52.isna().any() or not q.peakIn52.between(0, 100).all():
        raise ValueError('Missing/invalid observed career-window peak')
    q['peakBand'] = pd.cut(q.peakIn52, [-1, 0, 10, 40, 60, 100],
                           labels=['miss', '1-10', '11-40', '41-60', '61-100'])
    rows = []
    for (arm, seed, group), z in q.groupby(['arm', 'seed', 'group']):
        for band in q.peakBand.cat.categories:
            b = z[z.peakBand.eq(band)]
            observed = b[b.binding.notna()] if 'binding' in b else b.iloc[:0]
            rows.append(dict(arm=arm, seed=seed, group=group, peakBand=str(band),
                launches=len(b), allEligibleLaunches=len(z), share=len(b) / len(z),
                quality=float(b.quality.mean()), observedSupportRecords=len(observed),
                bindingShare=float(observed.binding.gt(0).mean()) if len(observed) else None,
                unprotectedFit=float(observed.unprotectedFit.median()) if len(observed) else None,
                protectedFit=float(observed.protectedFit.median()) if len(observed) else None))
    return pd.DataFrame(rows)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--review', required=True)
    args = ap.parse_args()
    out = Path(args.review)
    meta = json.loads((out / 'metadata.json').read_text(encoding='utf-8'))
    arms = list(meta['runs'])
    cohorts = pd.read_csv(out / 'cohort-summary.csv')
    releases = pd.read_csv(out / 'followup-releases.csv')
    annual = pd.read_csv(out / 'annual.csv')
    guards = pd.read_csv(out / 'guards.csv')
    years = sorted(annual.year.unique())
    hist = historical_chart(years)
    hist.to_csv(out / 'historical-chart.csv', index=False)
    bands = rank_bands(releases)
    bands.to_csv(out / 'followup-rank-bands.csv', index=False)
    hm = hist.mean(numeric_only=True)
    am = annual.groupby('arm').mean(numeric_only=True).reindex(arms)
    field = cohorts[cohorts.group.eq('field')]
    fm = field.groupby('arm').mean(numeric_only=True).reindex(arms)
    gm = guards.groupby('arm').mean(numeric_only=True).reindex(arms)
    hc = historical()
    hf = summary(hc[hc.group.eq('field') & hc.hitWeek.le(meta['weeks'] - 52)])
    lines = ['# Fit-floor chart discrepancies', '',
        f'Completed matched seeds: {meta["commonCompletedSeeds"]}; {meta["weeks"]} weeks per arm. All values below are means of seed metrics unless stated.', '',
        'A career improvement is an outcome to retain and investigate. These chart comparisons diagnose its costs; they do not automatically reject it.', '',
        '| Annual chart metric | Recovered history | ' + ' | '.join(arms) + ' |',
        '|---|---:|' + '---:|' * len(arms)]
    for key in CHART_KEYS:
        lines.append('| ' + key + ' | ' + f'{hm[key]:.3f}' + ' | ' + ' | '.join(f'{am.loc[arm,key]:.3f}' for arm in arms) + ' |')
    lines += ['', 'History uses the same calendar years, with carryovers included. Historical records are charted sides; the model has one represented side per single. Historical concentration uses reconstructed owners; model concentration uses selling firms. Do not read either as an exact apples-to-apples target.', '',
        '| Field career/follow-up metric | ' + ' | '.join(arms) + ' |', '|---|' + '---:|' * len(arms)]
    for key in ['acts', 'releasesPerAct', 'debutsPerAct', 'entry13Share', 'top10PerEntry', 'firstDebutGap',
                'actsWithFollowupTop40', 'recognitionGain', 'newEstablishedActs', 'everEstablishedActs',
                'promotedThenUnownedActs', 'unownedWeeks', 'belowEstablishedToEstablished', 'declinedAtEnd']:
        lines.append('| ' + key + ' | ' + ' | '.join(f'{fm.loc[arm,key]:.3f}' for arm in arms) + ' |')
    lines += ['', f'Historical early field cohort: {hf["acts"]} acts; {hf["chartEventsPerAct"]:.3f} chart-event proxies/act; {hf["top10PerEntry"]:.3f} Top 10s per subsequent charted side. Same-act entries within three weeks are merged into event proxies, not verified releases.', '',
        '| Seed, field outcomes | Arm | New Established / started below | Recognition gain | Annual 2+ Top 10 acts | Top 10 units, M | Field MidTier closures |',
        '|---|---|---:|---:|---:|---:|---:|']
    for seed in meta['commonCompletedSeeds']:
        for arm in arms:
            c = field[(field.seed == seed) & (field.arm == arm)].iloc[0]
            a = annual[(annual.seed == seed) & (annual.arm == arm)]
            g = guards[(guards.seed == seed) & (guards.arm == arm)].iloc[0]
            lines.append(f'| {seed} | {arm} | {int(c.newEstablishedActs)} / {int(c.startedBelowEstablishedActs)} | {c.recognitionGain:.3f} | {a.field_repeat2.mean():.1f} | {g.top10UnitsM:.3f} | {int(g.openingFieldMidClosures)} |')
    lines += ['', '| Eligible field follow-ups, peak by career endpoint | ' + ' | '.join(arms) + ' |', '|---|' + '---:|' * len(arms)]
    bm = bands[bands.group.eq('field')].groupby(['arm', 'peakBand']).share.mean()
    for band in ['miss', '61-100', '41-60', '11-40', '1-10']:
        lines.append('| ' + band + ' | ' + ' | '.join(f'{100 * bm.loc[(arm,band)]:.1f}%' for arm in arms) + ' |')
    lines += ['', 'This rank panel uses launches within the first 39 weeks after the hit. Peaks are observed through the common career endpoint, not each record\'s eventual lifetime peak. These are launches after the hit; the cohort debut metric also includes a record released before the milestone that enters afterward.', '',
        '| Direct support observation, field (pooled records) | Observed / eligible | Floor binds at least once |',
        '|---|---:|---:|']
    for arm in arms[1:]:
        q = releases[releases.arm.eq(arm) & releases.group.eq('field') & releases.entry13Eligible.eq(1)]
        for label, z in [('Top 10 follow-ups', q[q.peakIn52.between(1, 10)]), ('misses', q[q.peakIn52.eq(0)])]:
            observed = z[z.binding.notna()]
            lines.append(f'| {arm}: {label} | {len(observed)} / {len(z)} | {int(observed.binding.gt(0).sum())} |')
    lines += ['', 'A floor binding observation means the protected capture-time response exceeds the unprotected one in a traced region/week. The trace reads current owner/push and is not a demand-time causal replay. A Top 10 record with no observed binding does not demonstrate that support directly manufactured its success; earlier career/label survival and competition can still change the world.', '',
        '| Same-window economics | ' + ' | '.join(arms) + ' |', '|---|' + '---:|' * len(arms)]
    for key in ['singleUnitsM', 'top10UnitsM', 'r', 'actualBig5UnitShare', 'openingFieldMidClosures', 'matureRetiredLossShare', 'matureStillActive']:
        lines.append('| ' + key + ' | ' + ' | '.join(f'{gm.loc[arm,key]:.3f}' for arm in arms) + ' |')
    lines += ['', 'No individual artist is paired across worlds: a floor changes chart outcomes, contracts, release slates and cohort membership. Direct support binding in followup-rank-bands.csv is a capture-time read, not a resimulation with support removed. The simulation retains its existing budget, quality and regional response draws. No composite calibration score or new adoption threshold is imposed.', '']
    (out / 'chart-discrepancies.md').write_text('\n'.join(lines), encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
