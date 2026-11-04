"""Validate fanbase accounting and summarize completed fixed-window career screens.

Run analyze_hit_act_fit_support.py first with named off/base/fan/blend sources.
This reads its matched source metadata; it never reads an incomplete native world.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import path


def validate_stages(stages):
    if stages.empty:
        raise ValueError('Missing fanbase stage observations')
    required = ['rawDemand', 'fanAnonymousRaw', 'fanRaw', 'clearedFanUnits', 'clearedUnits', 'fanLaunchHeads', 'fanPurchasedToDate']
    if stages[required].isna().any().any() or stages[required].lt(-.001).any().any():
        raise ValueError('Missing/negative audience demand or ownership')
    tolerance = stages.rawDemand.abs() * 1e-5 + .05
    if ((stages.fanAnonymousRaw + stages.fanRaw - stages.rawDemand).abs() > tolerance).any():
        raise ValueError('Fan and anonymous raw demand do not reconcile')
    if (stages.clearedFanUnits > stages.clearedUnits + .02).any():
        raise ValueError('Attributed fan purchases exceed actual cleared units')
    if (stages.fanPurchasedToDate > stages.fanLaunchHeads + .1).any():
        raise ValueError('A title sold repeatedly beyond its launch audience')
    grouped = stages.sort_values('week').groupby(['recordId', 'region'])
    if grouped.fanLaunchHeads.nunique().gt(1).any():
        raise ValueError('A record refreshed its audience after launch')
    if grouped.fanPurchasedToDate.diff().lt(-.1).any():
        raise ValueError('Record audience ownership ran backward')
    return dict(stageRows=len(stages), observedTitles=int(stages.recordId.nunique()),
        rawReconciliationMax=float((stages.fanAnonymousRaw + stages.fanRaw - stages.rawDemand).abs().max()),
        tracedFanUnits=float(stages.clearedFanUnits.sum()), tracedAllUnits=float(stages.clearedUnits.sum()))


def validate_stock(fans):
    if fans.empty or fans.duplicated(['week', 'artistId']).any():
        raise ValueError('Missing/duplicate artist audience ledger')
    balance = fans.openingHeads + fans.recruitedHeads - fans.departedHeads
    if (fans.attachedHeads - balance).abs().gt(fans.attachedHeads.abs() * 1e-5 + .1).any():
        raise ValueError('Attached audience stock does not reconcile')
    if fans[['attachedHeads', 'newHeadsThisWeek', 'returningPurchasesThisWeek']].lt(-.01).any().any():
        raise ValueError('Negative fan stock or acquisitions')
    return dict(stockRows=len(fans), artists=int(fans.artistId.nunique()),
        stockReconciliationMax=float((fans.attachedHeads - balance).abs().max()),
        actualReturningUnits=float(fans.returningPurchasesThisWeek.sum()))


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--review', required=True)
    args = ap.parse_args()
    out = Path(args.review)
    meta = json.loads((out / 'metadata.json').read_text(encoding='utf-8'))
    cohorts = pd.read_json(out / 'cohorts.json')
    releases = pd.read_csv(out / 'followup-releases.csv')
    validation, rows, release_rows = [], [], []
    for arm, sources in meta['sources'].items():
        for seed_text, source in sources.items():
            seed = int(seed_text)
            p = lambda stream: Path(path(source['tag'], seed, stream, source['sourceWeeks']))
            if not p('artist-fanbase-weekly').exists():
                continue
            fans = pd.read_csv(p('artist-fanbase-weekly'))
            fans = fans[fans.week.le(meta['weeks'])]
            stages = pd.read_csv(p('single-demand-stages'))
            stages = stages[stages.week.le(meta['weeks'])]
            validation.append(dict(arm=arm, seed=seed, **validate_stock(fans), **validate_stages(stages)))
            acts = cohorts[(cohorts.arm == arm) & (cohorts.seed == seed)].copy()
            counts = fans.set_index(['artistId', 'week'])
            for hit in acts.itertuples():
                start = counts.loc[(hit.artistId, hit.hitWeek)] if (hit.artistId, hit.hitWeek) in counts.index else None
                end = counts.loc[(hit.artistId, hit.hitWeek + 52)] if (hit.artistId, hit.hitWeek + 52) in counts.index else None
                during = fans[fans.artistId.eq(hit.artistId) & fans.week.between(hit.hitWeek + 1, hit.hitWeek + 52)]
                peak = max(0 if start is None else float(start.attachedHeads), float(during.attachedHeads.max()) if len(during) else 0)
                end_heads = 0 if end is None else float(end.attachedHeads)
                rows.append(dict(arm=arm, seed=seed, group=hit.group, artistId=hit.artistId, hitWeek=hit.hitWeek,
                    headsAtHit=0 if start is None else float(start.attachedHeads),
                    heads52=end_heads, peakHeads52=peak, endToPeak=end_heads / peak if peak else 1.,
                    regions52=0 if end is None else int(end.activeRegions),
                    returningUnits52=float(during.returningPurchasesThisWeek.sum()),
                    recruits52=float(during.newHeadsThisWeek.sum())))
            eligible = releases[(releases.arm == arm) & (releases.seed == seed) & releases.entry13Eligible.eq(1)]
            observed = stages.merge(eligible[['singleId', 'week']].rename(columns={'week': 'launchWeek'}), left_on='recordId', right_on='singleId', validate='many_to_one')
            observed = observed[observed.week.between(observed.launchWeek, observed.launchWeek + 13)]
            by_record = observed.groupby('recordId').agg(fanUnits13=('clearedFanUnits', 'sum'), units13=('clearedUnits', 'sum'),
                fanRaw13=('fanRaw', 'sum'), raw13=('rawDemand', 'sum'))
            z = eligible.merge(by_record, left_on='singleId', right_index=True, validate='one_to_one')
            if z.units13.isna().any():
                raise ValueError('A mature follow-up has no demand attribution')
            release_rows.append(z)
    if not validation:
        raise ValueError('No fanbase worlds in the reviewed comparison')
    pd.DataFrame(validation).to_csv(out / 'fanbase-validation.csv', index=False)
    c = pd.DataFrame(rows); c.to_csv(out / 'fanbase-careers.csv', index=False)
    r = pd.concat(release_rows); r.to_csv(out / 'fanbase-followups.csv', index=False)
    lines = ['# Earned returning audience: completed screen', '',
        f'Matched seeds: {meta["commonCompletedSeeds"]}. Fixed observation: {meta["weeks"]} weeks.', '',
        'Audience equivalents are estimated regional/segment cohorts, not identified unique consumers. Acquisitions are from actual settled singles purchases with a material-response gate; records retain an immutable launch audience with saved purchase depletion. The first probe covers AI singles only.', '',
        '| Arm, field hit cohort | Heads at hit, mean | Heads after 52 weeks, mean | Endpoint / peak stock, mean | Regions at endpoint | Returning units / act, next 52 weeks |',
        '|---|---:|---:|---:|---:|---:|']
    # Average within seed before averaging seeds. Silence and declining artists remain in the cohort.
    means = c[c.group.eq('field')].groupby(['arm', 'seed']).mean(numeric_only=True).groupby('arm').mean()
    for arm, q in means.iterrows():
        lines.append(f'| {arm} | {q.headsAtHit:.0f} | {q.heads52:.0f} | {q.endToPeak:.1%} | {q.regions52:.2f} | {q.returningUnits52:.0f} |')
    lines += ['', '| Arm, eligible field follow-ups | Launches | Fan share of first-13-week cleared purchases | Entry in 13 weeks |', '|---|---:|---:|---:|']
    for arm, q in r[r.group.eq('field')].groupby('arm'):
        lines.append(f'| {arm} | {len(q)} | {q.fanUnits13.sum() / max(1,q.units13.sum()):.1%} | {q.entered13.mean():.1%} |')
    lines += ['', 'Stock identity, raw-component reconciliation, actual-cleared attribution, immutable launch pools and monotone title depletion all pass. These checks establish accounting, not historical calibration or a career treatment effect. Review career titles, recognition, follow-up ranks and economics in review.md and chart-discrepancies.md; do not select the largest hit count alone.', '']
    (out / 'fanbase-review.md').write_text('\n'.join(lines), encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
