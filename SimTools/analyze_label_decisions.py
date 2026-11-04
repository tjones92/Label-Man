"""Audit the choices a label makes before a release intention exists.

Read-only replay identity is mandatory. This reports the existing allocator's
reasoning and alternatives, not an optimal-profit policy or a historical target.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_followup_chart_trace import replay_identity
from analyze_label_balance import markdown_table
from analyze_studio_parent import BIG5, studio_ids


def choice_decisions(rows):
    if rows.empty or rows.duplicated(['week', 'slotId', 'artistId']).any():
        raise ValueError('Missing or duplicate candidate observations')
    results = []
    for (week, slot), q in rows.groupby(['week', 'slotId'], sort=False):
        for field in ['labelId', 'format', 'intendedWeek', 'financed']:
            if q[field].nunique(dropna=False) != 1:
                raise ValueError('Inconsistent slot context')
        eligible = q[q.eligible.eq(1)].sort_values('rank')
        selected = q[q.selected.eq(1)]
        if len(selected) != int(len(eligible) > 0):
            raise ValueError('Need one selected candidate exactly when candidates are eligible')
        if len(eligible):
            if eligible['rank'].tolist() != list(range(1, len(eligible) + 1)):
                raise ValueError('Incomplete candidate ranking')
            expected = eligible.sort_values(['score', 'artistId'], ascending=[False, True]).artistId.tolist()
            if eligible.artistId.tolist() != expected or selected.artistId.iloc[0] != expected[0]:
                raise ValueError('Selected candidate does not match the existing score ordering')
            if eligible[['score', 'expectedNet', 'contribution']].isna().any().any():
                raise ValueError('Missing eligible forecast')
        base = q.iloc[0]
        picked = selected.iloc[0] if len(selected) else None
        results.append(dict(week=week, slotId=slot, labelId=base.labelId, format=base['format'],
            intendedWeek=base.intendedWeek, eligibleCandidates=len(eligible), planned=int(picked is not None),
            artistId=None if picked is None else picked.artistId,
            careerState=None if picked is None else picked.careerState,
            selectedHot=None if picked is None else int(picked.hot),
            selectedDue=None if picked is None else int(picked.due),
            hotAvailable=int(eligible.hot.eq(1).any()), dueAvailable=int(eligible.due.eq(1).any()),
            newSigningAvailable=int(eligible.careerState.eq('NewSigning').any()),
            selectedNewSigning=None if picked is None else int(picked.careerState == 'NewSigning'),
            selectedNet=None if picked is None else picked.expectedNet,
            bestAvailableNet=None if picked is None else float(eligible.expectedNet.max()),
            selectedNonpositive=None if picked is None else int(picked.expectedNet <= 0),
            nonpositiveWithPositiveAlternative=None if picked is None else int(picked.expectedNet <= 0 and eligible.expectedNet.gt(0).any()),
            allNonpositive=None if picked is None else int(eligible.expectedNet.le(0).all()),
            higherNetPassedOver=None if picked is None else int(eligible.expectedNet.max() > picked.expectedNet + .01),
            selectedProfitContributionAtCap=None if picked is None else int(picked.contribution >= .999999),
            selectedAudience=None if picked is None else picked.attachedAudienceAtAudit))
    return pd.DataFrame(results)


def reconcile_proposals(decisions, slate, through):
    proposals = slate[slate.week.le(through) & slate.reason.eq('programme-review')].copy()
    if proposals.slotId.duplicated().any():
        raise ValueError('Duplicate initial release intentions')
    chosen = decisions[decisions.planned.eq(1)]
    if set(chosen.slotId) != set(proposals.slotId):
        raise ValueError('Choice decisions do not reconcile with initial intentions')
    paired = chosen.merge(proposals[['slotId', 'artistId', 'week']], on='slotId', validate='one_to_one', suffixes=('', '_proposal'))
    if paired.artistId.ne(paired.artistId_proposal).any() or paired.week.ne(paired.week_proposal).any():
        raise ValueError('Initial intention changed the selected artist or proposal week')
    # Later breakout reassignment is an outcome, not an error in the initial read.
    final = slate[slate.week.le(through)].drop_duplicates('slotId', keep='last')
    paired = paired.merge(final[['slotId', 'artistId', 'state', 'reason']], on='slotId', validate='one_to_one', suffixes=('', '_final'))
    paired['reassigned'] = paired.artistId.ne(paired.artistId_final)
    return paired


def summarize(decisions):
    rows = []
    for (group, fmt), q in decisions.groupby(['group', 'format']):
        planned = q[q.planned.eq(1)]
        hot = q[q.hotAvailable.eq(1)]
        due = q[q.dueAvailable.eq(1)]
        fresh = q[q.newSigningAvailable.eq(1)]
        rows.append(dict(group=group, format=fmt, slots=len(q), noEligibleShare=float(q.planned.eq(0).mean()),
            selectedHotShare=float(planned.selectedHot.mean()),
            hotSelectedWhenAvailable=float(hot.selectedHot.mean()),
            dueSelectedWhenAvailable=float(due.selectedDue.mean()),
            newSigningSelectedWhenAvailable=float(fresh.selectedNewSigning.mean()),
            selectedNonpositiveShare=float(planned.selectedNonpositive.mean()),
            nonpositiveWithPositiveAlternativeShare=float(planned.nonpositiveWithPositiveAlternative.mean()),
            allForecastsNonpositiveShare=float(planned.allNonpositive.mean()),
            higherNetPassedOverShare=float(planned.higherNetPassedOver.mean()),
            selectedProfitAtCapShare=float(planned.selectedProfitContributionAtCap.mean())))
    return pd.DataFrame(rows)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--reference', required=True)
    ap.add_argument('--trace', required=True)
    ap.add_argument('--weeks', type=int, default=106)
    ap.add_argument('--output', required=True)
    args = ap.parse_args()
    reference, trace, out = Path(args.reference), Path(args.trace), Path(args.output)
    identity = replay_identity(reference, trace, args.weeks - 1)
    p = lambda stream: str(trace) + '-' + stream + '.csv'
    candidates = pd.read_csv(p('programme-choices'))
    candidates = candidates[candidates.week.between(1, args.weeks)]
    decisions = choice_decisions(candidates)
    studios = studio_ids(p)
    group = lambda s: s.map(lambda x: 'big5' if x in BIG5 else 'studio' if x in studios else 'field')
    decisions['group'] = group(decisions.labelId)
    slate = pd.read_csv(p('release-slate'))
    paired = reconcile_proposals(decisions, slate, args.weeks)
    mature = paired[paired.week.le(args.weeks - 16)].copy()
    summary = summarize(decisions)
    appointments = pd.read_csv(p('daily-talent-appointments'))
    appointments = appointments[appointments.chartWeek.between(1, args.weeks)].copy()
    appointments['group'] = group(appointments.labelId)
    ar = []
    for g, q in appointments.groupby('group'):
        accepted = q[q.offerOutcome.isin(['AcceptedUncontested', 'AcceptedArtistChoice'])]
        ar.append(dict(group=g, appointments=len(q), accepted=len(accepted),
            acceptedFresh=int(accepted.selectedLane.eq('FreshPotential').sum()),
            acceptedExperienced=int(accepted.selectedLane.eq('ExperiencedProduction').sum()),
            lostArtistChoice=int(q.offerOutcome.eq('LostArtistChoice').sum()),
            noQualifyingCandidate=int(q.offerOutcome.eq('NoQualifyingCandidate').sum())))
    ar = pd.DataFrame(ar)
    out.mkdir(parents=True, exist_ok=True)
    decisions.to_csv(out / 'decisions.csv', index=False)
    summary.to_csv(out / 'choice-summary.csv', index=False)
    paired.to_csv(out / 'selected-intentions.csv', index=False)
    mature.to_csv(out / 'mature-intentions.csv', index=False)
    ar.to_csv(out / 'ar-summary.csv', index=False)
    (out / 'identity.json').write_text(json.dumps(identity, indent=2), encoding='utf-8')
    lines = ['# Label decision audit', '',
        f'Observed planning weeks 1–{args.weeks}. Replay identity: {len(identity["identicalWeekKeyedStreams"])} checked streams through week {args.weeks - 1}.', '',
        'The audit retains scored alternatives, eligibility exclusions and empty opportunities. Selected means an initial intention, not a released or successful record. Attached audience is observational; the current allocator does not use it as a decision input.', '',
        markdown_table(summary.round(3)), '',
        'Higher forecast profit passed over is descriptive: obligations, hit-act urgency and development may justify it. A capped profit contribution is a property of the existing score, not a test that every choice is wrong. These figures do not define a profit-maximizing policy.', '',
        markdown_table(ar), '',
        'A&R counts daily appointments, not distinct candidates or continuous searches. Their current vacancy gate can prevent an appointment entirely; that absence is outside this table. Each recorded initial intention reconciles with its selected artist and planning week. Later reassignments and unresolved intentions remain in the ledgers.']
    report = '\n'.join(lines) + '\n'
    (out / 'review.md').write_text(report, encoding='utf-8')
    print(report)


if __name__ == '__main__':
    main()
