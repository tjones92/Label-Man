"""Separate national-component and regional-input effects in recorded worlds.

Both screens remove surviving original stock only, using observed references and
independent hysteresis. The combined replay must reproduce the C# origin shadow.
"""
import argparse
import csv
import gzip
import json
from collections import Counter, defaultdict
from pathlib import Path

from trace_stature_marketing_mechanisms import rows
from stature_normalization import file_digest, parse_date
from stature_shadow_policy import evaluate, BANDS


def optional(value):
    return float(value) if value != '' else None


def maximum(values):
    return max(values) if all(v is not None for v in values) else None


def policy(config):
    source = config['Policy']
    return {'version': source['Version'], 'effectiveDate': source['EffectiveDate'][:10],
        'bands': {name: {'entry': r['Entry'], 'exit': r['Exit'], 'reachWeight': dict(zip(('regional', 'record', 'conversion'), r['ReachWeights'])),
                        **({'eligibleRoutes': r['EligibleRoutes']} if r.get('EligibleRoutes') is not None else {})}
                  for name, r in zip(BANDS, source['Bands'])}}


def evidence(snapshot, origin, regional=False, national=False):
    sales = maximum([optional(snapshot[k + 'Score']) for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult')])
    tv = maximum([optional(snapshot[k + 'Score']) for k in ('tvYoung', 'tvAdult')])
    following = []
    for age in ('Young', 'Adult'):
        score = optional(snapshot['following' + age + 'Score'])
        reference = optional(origin['following' + age + 'Reference'])
        if national:
            if score is None or reference is None:
                score = None
            else:
                # Unclipped values are exactly reconstructible. For saturated
                # components, the stock CSV's float national sum is used; verify
                # every combined band against the C# raw-input calculation.
                raw = score * reference if score < 1 else float(origin['following' + age])
                score = min(1, max(0, raw - float(origin['opening' + age])) / reference)
        following.append(score)
    draw = maximum(following)
    prefix = 'zero' if regional else 'actual'
    record_reach = optional(origin[prefix + 'RecordReach'])
    return {'regional': {'response': optional(snapshot['regionalResponse']), 'draw': optional(origin[prefix + 'RegionalDraw']), 'reach': record_reach},
            'record': {'response': sales, 'draw': draw, 'reach': record_reach},
            'conversion': {'response': tv, 'draw': draw, 'reach': optional(origin[prefix + 'ConversionReach'])}}


def selected_shadow(directory, name, artists):
    p = directory / f'{name}-stature-shadow.csv'
    opener = open
    if not p.exists():
        p = Path(str(p) + '.gz'); opener = gzip.open
    with opener(p, 'rt', encoding='utf-8-sig', newline='') as f:
        reader = csv.reader(f); fields = next(reader); artist_index = fields.index('artistId')
        for r in reader:
            if r[artist_index] in artists:
                yield dict(zip(fields, r))


def analyze(directory, name, calibration, artists=None):
    origins = {(parse_date(r['date']).isoformat(), r['artistId']): r for r in rows(directory, name, 'opening-following-shadow')
               if artists is None or r['artistId'] in artists}
    seeded = {key[1] for key in origins}
    previous, observed, counts, affected = {}, {}, defaultdict(Counter), defaultdict(set)
    for s in selected_shadow(directory, name, seeded):
        key = s['snapshotDate'], s['artistId']
        if not key[0] or key not in origins:
            continue
        origin = origins[key]
        date, artist = key
        for variant, regional, national in (('national_only', False, True), ('regional_only', True, False), ('combined', True, True)):
            prior = previous.get((variant, artist))
            value = evaluate(evidence(s, origin, regional, national), calibration, date, prior)
            band = value['band'] or 'BelowRising'
            status = value['status']
            if variant == 'combined':
                actual_status = {'observed': 'Observed', 'below_rising': 'BelowRising', 'missing_evidence': 'MissingEvidence'}[status]
                if actual_status != origin['zeroOpeningStatus'] or actual_status != 'MissingEvidence' and band != origin['zeroOpeningBand']:
                    raise ValueError(f'Combined raw-source reconstruction differs: {key}')
            if status != 'missing_evidence':
                previous[variant, artist] = value['band']
            observed[variant, date, artist] = (band, status)
            c = counts[int(date[:4]), variant]
            c['observations'] += 1
            if status == 'missing_evidence' or s['status'] == 'MissingEvidence':
                c['missingComparisons'] += 1
            else:
                lower = ('BelowRising', *BANDS).index(band) < ('BelowRising', *BANDS).index(s['shadowBand'])
                c['lowerBand'] += lower
                if lower:
                    affected[int(date[:4]), variant].add(artist)
    budgets, acts = defaultdict(Counter), defaultdict(set)
    for r in rows(directory, name, 'opening-following-marketing'):
        if r['artistId'] not in seeded:
            continue
        if r['context'] != 'release-launch' or r['status'] != 'Available':
            continue
        old, proposed = float(r['legacyBudget']), float(r['actualProposedBudget'])
        for variant in ('national_only', 'regional_only', 'combined'):
            c = budgets[parse_date(r['date']).year, variant]
            c['actualUplifts'] += proposed > old
            band, status = observed[variant, r['snapshotDate'], r['artistId']]
            if status == 'missing_evidence':
                c['missingComparisons'] += 1; continue
            multipliers = {'Superstar': 2.5, 'Star': 2, 'Established': 1.5, 'Rising': 1.2, 'NewSigning': .8}
            career = band if band != 'BelowRising' else 'NewSigning' if r['legacyCareer'] == 'NewSigning' else 'Declining'
            budget = old * multipliers.get(career, .5) / multipliers.get(r['legacyCareer'], .5)
            c['availableReads'] += 1
            reduced = proposed > old and budget < proposed - max(.001, proposed * 2e-6)
            c['upliftReduced'] += reduced
            c['upliftRemoved'] += proposed > old and budget <= old + max(.001, old * 2e-6)
            if reduced:
                acts[parse_date(r['date']).year, variant].add(r['artistId'])
    return {'combinedMatchesCSharpEveryObservation': True,
        'bandObservations': [{'year': y, 'variant': v, **c, 'distinctLowerBandActs': len(affected[y, v])} for (y, v), c in sorted(counts.items())],
        'launchBudgetReads': [{'year': y, 'variant': v, **c, 'distinctUpliftReducedActs': len(acts[y, v])} for (y, v), c in sorted(budgets.items())]}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--prefix', required=True)
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002])
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    config = Path('SimTools/StatureShadowExperimental.json')
    report = {'analysisSha256': file_digest(Path(__file__)), 'calibrationSha256': file_digest(config),
        'scope': 'Source removal from national components only, regional draw/reach inputs only, or both. Observed references, sales/TV, actual audience and budgets stay fixed. Independent hysteresis; these are overlapping pathways, not additive causal shares.', 'seeds': {}}
    for seed in a.seeds:
        report['seeds'][str(seed)] = {arm: analyze(a.directory, f'{a.prefix}-{seed}-{arm}', policy(json.loads(config.read_text()))) for arm in ('control', 'treatment')}
    with a.output.open('x', encoding='utf-8') as f:
        json.dump(report, f, indent=2); f.write('\n')
