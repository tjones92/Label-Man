"""Inspect complete within-world careers, including attached audiences that stall.

Cases are the median starting-audience act in each stated outcome bucket, not
individual treatment effects or a selection of only successful simulated careers.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import path


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--review', required=True)
    args = ap.parse_args()
    out = Path(args.review)
    meta = json.loads((out / 'metadata.json').read_text())
    career = pd.read_csv(out / 'career-progress.csv')
    fans = pd.read_csv(out / 'fanbase-careers.csv')
    releases = pd.read_csv(out / 'fanbase-followups.csv')
    all_releases = pd.read_csv(out / 'followup-releases.csv')
    joined = career.merge(fans, on=['arm', 'seed', 'group', 'artistId'], validate='one_to_one')
    field = joined[joined.group.eq('field') & joined.firstCareerTop10.eq(1)].copy()
    counts, cases = [], []
    for arm, q in field.groupby('arm'):
        buckets = {
            'progressed and held at endpoint': q[q.newEstablished.eq(1)],
            'promoted then unowned': q[q.promotedThenUnowned.eq(1)],
            'audience stock above first-hit level but declining': q[q.declinedAtEnd.eq(1) & q.heads52.ge(q.headsAtHit)],
            'lost at least 20% of peak audience stock': q[q.endToPeak.lt(.8)],
        }
        for kind, z in buckets.items():
            counts.append(dict(arm=arm, kind=kind, acts=len(z), fullFirstCareerCohort=len(q)))
            if len(z):
                chosen = z.sort_values(['headsAtHit', 'seed', 'artistId']).iloc[len(z) // 2]
                cases.append((kind, chosen))
    pd.DataFrame(counts).to_csv(out / 'fanbase-case-buckets.csv', index=False)
    lines = ['# Within-world career sequences', '',
        'Complete first-career-hit proxy cohorts, field labels only. Buckets keep failed careers visible. '
        'The shown act is the median by audience at the milestone within its bucket; it is not a paired cross-world artist or proof of a causal pathway.', '',
        '| Arm | Outcome bucket | Acts | Full cohort |', '|---|---|---:|---:|']
    for c in counts:
        lines.append(f'| {c["arm"]} | {c["kind"]} | {c["acts"]} | {c["fullFirstCareerCohort"]} |')
    exported = []
    for kind, c in cases:
        source = meta['sources'][c.arm][str(int(c.seed))]
        p = lambda stream: path(source['tag'], int(c.seed), stream, source['sourceWeeks'])
        chart = pd.read_csv(p('records'), usecols=['recordId', 'title', 'week', 'currentPosition'])
        titles = chart.drop_duplicates('recordId').set_index('recordId').title
        stages = pd.read_csv(p('single-demand-stages'))
        initial = stages.drop_duplicates(['recordId', 'region']).groupby('recordId').fanLaunchHeads.sum()
        actor = all_releases[all_releases.arm.eq(c.arm) & all_releases.seed.eq(c.seed) & all_releases.artistId.eq(c.artistId)].sort_values('week')
        trace = pd.read_csv(p('followup-careers'))
        trace = trace[trace.artistId.eq(c.artistId) & trace.week.between(c.hitWeek, c.hitWeek + 52)].sort_values('week')
        transitions = trace[trace.careerState.ne(trace.careerState.shift())]
        lines += ['', f'## {c.arm}, seed {int(c.seed)}, {c.artistId}: {kind}', '',
            f'First observed hit week {int(c.hitWeek)}; {c.startState} -> {c.endState}. '
            f'Attached audience equivalents {c.headsAtHit:,.0f} -> {c.heads52:,.0f}; '
            f'endpoint/peak stock {c.endToPeak:.1%}; returning purchases {c.returningUnits52:,.0f} over the next 52 weeks. '
            f'Recognition {c.startRecognition:.3f} -> {c.endRecognition:.3f}.', '',
            'Career-state sequence: ' + '; '.join(f'week {int(t.week)} {t.careerState}' for t in transitions.itertuples()) + '.', '',
            '| Launch week | Title | Quality | Launch audience | Peak by career endpoint | Returning / all cleared purchases, first 13 weeks |',
            '|---|---|---:|---:|---:|---:|']
        for r in actor.itertuples():
            z = releases[releases.arm.eq(c.arm) & releases.seed.eq(c.seed) & releases.singleId.eq(r.singleId)]
            purchases = f'{z.fanUnits13.iloc[0]:,.0f} / {z.units13.iloc[0]:,.0f}' if len(z) else 'incomplete 13-week window'
            peak = 'miss' if r.peakIn52 == 0 else str(int(r.peakIn52))
            title = titles.get(r.singleId, r.singleId).replace('|', '/')
            lines.append(f'| {int(r.week)} | {title} | {r.quality:.3f} | {initial.get(r.singleId,0):,.0f} | {peak} | {purchases} |')
            exported.append(dict(arm=c.arm, seed=int(c.seed), artistId=c.artistId, kind=kind, singleId=r.singleId,
                week=r.week, title=title, quality=r.quality, launchAudience=float(initial.get(r.singleId,0)), peakIn52=r.peakIn52))
    lines += ['', 'Audience stocks are inferred cohorts. Career credits can include album milestones and a record already in flight at the first-hit milestone; the table shows singles launched afterward. The returning/all purchase window is only reported for launches with a complete next 13 weeks. A still-attached audience alongside decline is evidence to investigate label/career readers, not permission to assign a better title automatically.', '']
    pd.DataFrame(exported).to_csv(out / 'fanbase-case-releases.csv', index=False)
    (out / 'fanbase-career-cases.md').write_text('\n'.join(lines), encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
