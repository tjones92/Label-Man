"""Validate and compare the previous policy, connected material systems, and reviewed projects."""
import argparse
import json
import math
from pathlib import Path

import pandas as pd

from analyze_followup_chart_trace import replay_identity
from analyze_followup_chart_funnel import summary
from analyze_hit_act_fit_support import measure
from analyze_offchart_screen import ROOT
from analyze_studio_parent import BIG5, studio_ids

CONNECTIONS = {'--anticipatory-material-search', '--release-judgement-shadow', '--release-judgement-select=perceived'}


def validate_manifests(manifests, seed, weeks):
    base = set(manifests['control']['extraFlags'])
    for arm, extra in [('control', set()), ('connected', CONNECTIONS), ('judged', {'--label-project-judgement'})]:
        m = manifests[arm]
        completed = [r for r in m['results'] if r['seed'] == seed and r['passed'] and r['exitCode'] == 0]
        if m['weeks'] != weeks or m['profile'] != 'game' or len(completed) != 1 or set(m['extraFlags']) != base | extra:
            raise ValueError(f'{arm}: incomplete or mismatched configuration')
    if len({m['assemblySha256'] for m in manifests.values()}) != 1:
        raise ValueError('Arms used different assemblies')


def validate_reviews(reviews):
    recording = reviews.action.eq('record-reviewed-material')
    if not reviews[['perceived', 'routineRead']].map(lambda x: math.isfinite(x)).all().all():
        raise ValueError('Invalid material evidence')
    # A shortfall inside the label's recorded read uncertainty is not evidence of weaker material.
    tolerance = reviews['uncertainty'].clip(lower=0) if 'uncertainty' in reviews else 0
    if (reviews.loc[recording, 'perceived'] < (reviews['routineRead'] - tolerance)[recording] - 1e-6).any():
        raise ValueError('A session was approved below its material standard')
    if reviews.loc[recording, 'candidateId'].isna().any():
        raise ValueError('An approved session has no material identity')
    own = reviews.action.eq('record-own-session')
    if own.any() and not reviews.loc[own, 'candidateId'].astype(str).str.startswith('routine-session|').all():
        raise ValueError('An own session names outside material')
    if not reviews.action.isin(['record-reviewed-material', 'record-own-session', 'hold-for-stronger-material', 'await-material-delivery']).all():
        raise ValueError('Unknown project action')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--tag', required=True)
    ap.add_argument('--reference', required=True)
    ap.add_argument('--seed', type=int, default=1001)
    ap.add_argument('--weeks', type=int, default=106)
    args = ap.parse_args()
    logs = Path(ROOT)
    manifests = {a: json.loads((logs / f'{args.tag}-{a}' / 'runs.json').read_text(encoding='utf-8-sig')) for a in ('control', 'connected', 'judged')}
    validate_manifests(manifests, args.seed, args.weeks)
    reference = json.loads((logs / args.reference / 'runs.json').read_text(encoding='utf-8-sig'))
    if set(reference['extraFlags']) != set(manifests['control']['extraFlags']):
        raise ValueError('Archived reference differs from the replay control')
    identity = replay_identity(logs / f'{args.reference}-default-{args.seed}-{reference["weeks"]}',
                               logs / f'{args.tag}-control-default-{args.seed}-{args.weeks}', args.weeks - 1)
    out = logs / args.tag / 'review'
    out.mkdir(parents=True, exist_ok=True)
    annual_rows, guard_rows, behavior_rows, cohort_rows = [], [], [], []
    for arm in manifests:
        tag = f'{args.tag}-{arm}'
        prefix = logs / f'{tag}-default-{args.seed}-{args.weeks}'
        cohorts, releases, careers, annual, guards = measure(tag, args.seed, args.weeks, args.weeks)
        for name, c in cohorts.groupby('group'):
            cohort_rows.append(dict(arm=arm, group=name, **summary(c, releases[releases.group.eq(name)])))
        annual_rows.extend(dict(arm=arm, year=y, **r) for y, r in annual.items())
        guard_rows.append(dict(arm=arm, **guards))
        p = lambda s: str(prefix) + f'-{s}.csv'
        studios = studio_ids(p)
        group = lambda x: 'big5' if x in BIG5 else 'studio' if x in studios else 'field'
        slate = pd.read_csv(p('release-slate'))
        singles = slate[slate['format'].eq('Single')]
        latest = singles.drop_duplicates('slotId', keep='last')
        review_path = Path(p('programme-projects'))
        reviews = pd.read_csv(review_path) if review_path.exists() else pd.DataFrame()
        if len(reviews):
            validate_reviews(reviews)
            reviews['group'] = reviews.labelId.map(group)
        for name in ('big5', 'studio', 'field'):
            q = latest[latest.labelId.map(group).eq(name)]
            r = reviews[reviews.group.eq(name)] if len(reviews) else reviews
            behavior_rows.append(dict(arm=arm, group=name, singleIntentions=len(q), issued=int(q.state.eq('Issued').sum()),
                cancelled=int(q.state.eq('Cancelled').sum()), open=int((~q.state.isin(['Issued', 'Cancelled'])).sum()),
                heldProjects=int(r.loc[r.action.eq('hold-for-stronger-material'), 'slotId'].nunique()) if len(r) else 0,
                heldHitFollowups=int(r.loc[r.action.eq('hold-for-stronger-material') & r.source.eq('hit-follow-up'), 'slotId'].nunique()) if len(r) else 0,
                searchedApprovals=int(r.loc[r.action.eq('record-reviewed-material') & r.useSearchedMaterial.eq(1), 'slotId'].nunique()) if len(r) else 0,
                ownSessions=int(r.loc[r.action.eq('record-own-session'), 'slotId'].nunique()) if len(r) else 0))
    annual = pd.DataFrame(annual_rows)
    guards = pd.DataFrame(guard_rows)
    behavior = pd.DataFrame(behavior_rows)
    if len(reviews):
        # Keep whole histories for examples of waiting and later authorizing a project, rather than
        # presenting the last row as if every hold cancelled or every approval reached the market.
        held = reviews[reviews.action.eq('hold-for-stronger-material')].groupby('slotId').week.min()
        approved = reviews[reviews.action.isin(['record-reviewed-material', 'record-own-session'])].groupby('slotId').week.min()
        recovered = held.index.intersection(approved.index)
        recovered = [s for s in recovered if held[s] < approved[s]]
        examples = []
        for source in ('hit-follow-up', 'contract-delivery', 'roster-programme'):
            eligible = reviews[reviews.slotId.isin(recovered) & reviews.source.eq(source)].slotId.drop_duplicates().head(2)
            examples.extend(eligible)
        reviews[reviews.slotId.isin(examples)].to_csv(out / 'project-examples.csv', index=False)
        reviews.to_csv(out / 'project-reviews.csv', index=False)
    if guards[['basePurchaseCapacity', 'albumChannelCapacity']].nunique().gt(1).any():
        raise ValueError('Authored consumer capacity changed')
    annual.to_csv(out / 'annual.csv', index=False)
    guards.to_csv(out / 'guards.csv', index=False)
    behavior.to_csv(out / 'behavior.csv', index=False)
    pd.DataFrame(cohort_rows).to_csv(out / 'cohort-summary.csv', index=False)
    (out / 'identity.json').write_text(json.dumps(identity, indent=2), encoding='utf-8')
    lines = ['# Connected label projects: first screen', '', f'Seed {args.seed}; {args.weeks} weeks. Same assembly and economic configuration across arms.',
        'Control replay identity is checked before treatment comparisons. This is a behavioral screen, not adoption or evidence of calibrated historical balance.', '',
        '| Annual mean | Control | Connected | Judged |', '|---|---:|---:|---:|']
    means = annual.groupby('arm').mean(numeric_only=True)
    for key in ('big5_repeat2', 'studio_repeat2', 'field_repeat2', 'big5_top10Share', 'studio_top10Share', 'field_top10Share'):
        lines.append('| ' + key + ' | ' + ' | '.join(f'{means.loc[a, key]:.3f}' for a in manifests) + ' |')
    lines += ['', 'Project counts include open/censored intentions. A hold is a dated evidence-based review, not a cancellation target.', '', behavior.to_string(index=False), '', guards.to_string(index=False)]
    (out / 'review.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
