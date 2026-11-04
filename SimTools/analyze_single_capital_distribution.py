"""Validate v3 candidate distributions and compare observe, mean-only and log policies.

python SimTools/analyze_single_capital_distribution.py --tag scr-v3 --seeds 1001
One-seed annual results are a mechanism screen, not calibration of loss probabilities or risk preferences.
"""
import argparse
import json

import numpy as np
import pandas as pd

from analyze_single_capital_risk import ROOT, annual, diagnose_outcomes, manifest, prefix, read, replay


def validate_risk(q, policy):
    if q.empty or not q.modelVersion.eq(3).all() or not q.policy.eq(policy).all():
        raise ValueError('Missing or wrong distribution model')
    if not np.isfinite(q.select_dtypes(include='number').to_numpy()).all():
        raise ValueError('Nonfinite risk arithmetic')
    expected = q.expectedContribution - q.futureCost
    tolerance = np.maximum(1, np.abs(expected)) * 1e-7
    if (np.abs(q.distributionMean - expected) > tolerance).any() or (np.abs(q.meanError) > tolerance).any():
        raise ValueError('Distribution mean differs from the candidate forecast')
    if (np.abs(q.forecastNet - expected) > tolerance * 2).any():
        raise ValueError('Forecast uses another cost basis')
    if not np.allclose(q.lossFloor, -q.futureCost) or (q.minNet < q.lossFloor - tolerance).any():
        raise ValueError('Candidate loss exceeds unpaid commitments')
    if (q.capital <= 0).any() or (q.riskPremium < 0).any():
        raise ValueError('Ordinary funding failure misclassified as risk')
    if q.loc[q.prepared, 'recording'].ne(0).any() or (q.recording < 0).any():
        raise ValueError('Paid recording charged again')
    if not np.allclose(q.futureCost, q.recording + q.marketing + q.pressing):
        raise ValueError('Commitment components do not reconcile')
    if not q.lossProbability.between(0, 1).all() or not q.exhaustionProbability.between(0, 1).all():
        raise ValueError('Invalid downside probabilities')
    if (q.exhaustionProbability > q.lossProbability + 1e-8).any():
        raise ValueError('Exhaustion without a loss')
    if (q.ready & ((q.effectiveSamples < 4 - 1e-8) | (q.samples < 4))).any():
        raise ValueError('Insufficient evidence classified as an empirical shape')
    feasible = q.ready & q.exhaustionProbability.eq(0)
    if (q.loc[feasible, 'adjustedNet'] > expected[feasible] + tolerance[feasible]).any():
        raise ValueError('Log certainty equivalent exceeds the arithmetic mean')
    if (q.logCommit & ~q.meanCommit).any() or (q.meanCommit != expected.gt(0)).any():
        raise ValueError('Mean and log gates do not share their forecast')
    if (q.forecastClamped & q.meanCommit).any():
        raise ValueError('Clamping an impossible receipt forecast created a positive mean')
    all_wins = q.ready & q.lossProbability.eq(0) & q.meanCommit
    if (~q.loc[all_wins, 'logCommit']).any():
        raise ValueError('Profitable candidate support rejected by transferred dispersion')
    chosen = pd.Series(True, index=q.index) if policy == 'observe' else q.meanCommit if policy == 'mean' else q.logCommit
    if (q.commit != chosen).any():
        raise ValueError('Wrong policy applied')
    return dict(assessments=len(q), maxMeanError=float(q.meanError.abs().max()),
                lossFloorViolations=0, noLossRiskRefusals=0, ordinaryCashFailuresInRiskLedger=0,
                allProfitableAssessments=int(all_wins.sum()), preparedAssessments=int(q.prepared.sum()),
                clampedForecasts=int(q.forecastClamped.sum()))


def evidence_rows(q):
    rows = []
    for (tier, prepared), a in q.groupby(['tier', 'prepared']):
        empirical = a[a.ready]
        last = a.sort_values('week').drop_duplicates('labelId', keep='last')
        risk_only = a[a.meanCommit & ~a.logCommit]
        rows.append(dict(tier=tier, prepared=prepared, attempts=len(a), slots=a.slotId.nunique(), labels=a.labelId.nunique(),
            readyShare=a.ready.mean(), lastReadyLabels=int(last.ready.sum()), lastMedianEffectiveSamples=last.effectiveSamples.median(),
            meanRefusedAttempts=int((~a.meanCommit).sum()), riskOnlyAttempts=len(risk_only), riskOnlySlots=risk_only.slotId.nunique(),
            actualRefusedAttempts=int((~a.commit).sum()), insufficientAttempts=int(a.status.eq('insufficient-history').sum()),
            zeroContributionAttempts=int(a.status.eq('zero-contribution-history').sum()),
            medianEmpiricalLossProbability=empirical.lossProbability.median(), medianEmpiricalPremium=empirical.riskPremium.median(),
            medianEmpiricalForecast=empirical.forecastNet.median(), medianCapitalMonths=(a.capital / a.overhead).median()))
    return rows


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--tag', default='scr-v3')
    ap.add_argument('--reference', default='erc-v2-w17')
    ap.add_argument('--seeds', nargs='+', type=int, default=[1001])
    args = ap.parse_args()
    plan = json.loads((ROOT / (args.tag + '-checks') / 'plan.json').read_text(encoding='utf-8-sig'))
    if plan['modelVersion'] != 3 or not all(c['passed'] for c in plan['checks']) or len(plan['checks']) != 2:
        raise ValueError('Native and enabled round-trip checks required')
    policies = plan['policies']
    if set(policies) != {'observe', 'mean', 'log'}:
        raise ValueError('Complete all three policies for the matched screen')
    manifests = {p: manifest(args.tag + '-' + p) for p in policies}
    reference = manifest(args.reference)
    through = ((min(m['weeks'] for m in manifests.values()) - 1) // 52) * 52
    if through < 52 or reference['weeks'] <= through:
        raise ValueError('Need complete and matched annual windows')
    for policy, m in manifests.items():
        common = [f for f in m['extraFlags'] if not f.startswith(('--single-capital-risk=', '--single-capital-risk-policy='))]
        dose = [f for f in m['extraFlags'] if f.startswith('--single-capital-risk=')]
        if common != reference['extraFlags'] or len(dose) != 1 or float(dose[0].split('=', 1)[1]) != plan['capitalFraction']:
            raise ValueError('Mismatched bundle or capital fraction')
        if '--single-capital-risk-policy=' + policy not in m['extraFlags'] or m['assemblySha256'] != plan['assemblySha256']:
            raise ValueError('Wrong policy or assembly')
    out = ROOT / args.tag / 'review'
    out.mkdir(parents=True, exist_ok=True)
    yearly, evidence, outcomes, examples, validations = [], [], [], [], []
    for seed in args.seeds:
        old = prefix(args.reference, seed, reference['weeks'])
        yearly.extend(dict(arm='archived', seed=seed, **r) for r in annual(old, through))
        base_capacity = read(old, 'market-clearing-weekly', usecols=['week', 'regionId', 'baseCapacity', 'albumChannelCapacity'])
        base_capacity = base_capacity[base_capacity.week <= through].sort_values(['week', 'regionId']).reset_index(drop=True)
        for policy, m in manifests.items():
            matches = [r for r in m['results'] if r['seed'] == seed and r['passed'] and r['exitCode'] == 0]
            if len(matches) != 1:
                raise ValueError('Incomplete policy run')
            p = prefix(args.tag + '-' + policy, seed, m['weeks'])
            yearly.extend(dict(arm=policy, seed=seed, **r) for r in annual(p, through))
            c = read(p, 'market-clearing-weekly', usecols=list(base_capacity.columns))
            pd.testing.assert_frame_equal(base_capacity, c[c.week <= through].sort_values(['week', 'regionId']).reset_index(drop=True))
            q = read(p, 'single-capital-risk')
            q = q[q.week <= through]
            validation = validate_risk(q, policy)
            final = read(p, 'single-capital-risk-outcomes')
            validation['recordedFinals'] = len(final)
            validation['negativeFinalContributions'] = int(final.finalContribution.lt(0).sum())
            validation['nonpositivePriorContributions'] = int(final.expectedContribution.le(0).sum())
            validations.append(dict(policy=policy, seed=seed, **validation))
            evidence.extend(dict(policy=policy, seed=seed, **r) for r in evidence_rows(q))
            outcomes.extend(dict(policy=policy, seed=seed, **r) for r in diagnose_outcomes(p, through))
            e = q[q.ready & q.meanCommit & ~q.logCommit].sort_values(['tier', 'week']).drop_duplicates(['tier', 'prepared'])
            examples.append(e.assign(seed=seed))
        # Observe carries saved/diagnostic metadata only: every old weekly stream must still match through w52.
        observe_identity = replay(old, prefix(args.tag + '-observe', seed, manifests['observe']['weeks']), through)
        validations.append(dict(policy='observe-replay', seed=seed, identicalStreams=len(observe_identity['identicalStreams'])))
    off_identity = replay(prefix(args.reference, args.seeds[0], reference['weeks']), prefix(args.tag + '-off13', args.seeds[0], 13), 12)
    a = pd.DataFrame(yearly)
    e = pd.DataFrame(evidence)
    v = pd.DataFrame(validations)
    for name, frame in [('annual', a), ('evidence', e), ('outcomes', pd.DataFrame(outcomes)), ('validation', v),
                        ('examples', pd.concat(examples, ignore_index=True))]:
        frame.to_csv(out / (name + '.csv'), index=False)
    metadata = dict(modelVersion=3, seeds=args.seeds, throughWeek=through, assemblySha256=plan['assemblySha256'],
        offReplayStreams=len(off_identity['identicalStreams']), observeReplayThrough=through,
        caution='Forecast-consistent distribution, not validation of the mean forecast or a historical risk preference. Sparse shapes fall back to mean.')
    (out / 'metadata.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    print('Annual\n' + a.round(4).to_string(index=False))
    print('\nEvidence\n' + e.round(4).to_string(index=False))
    print('\nValidation\n' + v.round(8).to_string(index=False))
    print(json.dumps(metadata, indent=2))


if __name__ == '__main__':
    main()
