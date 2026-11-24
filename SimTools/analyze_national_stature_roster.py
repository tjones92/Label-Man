"""Replay national high-tier eligibility across the retained full AI rosters."""
import argparse
import json
import math
from collections import Counter, defaultdict
from datetime import date, timedelta
from functools import lru_cache
from pathlib import Path

from analyze_regional_standing_sensitivity import evidence, quote
from compress_stature_outputs import csv_digest
from partition_opening_following_routes import optional, policy
from replay_opening_geography import ORDER, STATUS
from stature_normalization import file_digest, parse_date
from stature_shadow_policy import BANDS, evaluate
from trace_stature_marketing_mechanisms import rows
from verify_national_stature_policy import candidate_is_scoped

HIGH = {'Star', 'Superstar'}


def check_legacy(row, result, prior):
    status = STATUS[result['status']]
    band = prior if status == 'MissingEvidence' else result['band']
    if (status, band or 'BelowRising', prior or 'BelowRising') != (row['status'], row['shadowBand'], row['previousBand']):
        raise ValueError(f"Legacy replay differs from C# for {row['artistId']} on {row['snapshotDate']}")
    for b, support in result['bands'].items():
        values = {'Lower': support['lower'], 'Upper': support['upper'], 'Boundary': support['boundary'],
                  **{r.title(): s['lower'] for r, s in support['routes'].items()}}
        if any(abs(v - float(row[b + field])) > 1e-12 for field, v in values.items()):
            raise ValueError(f"Legacy support differs from C# for {row['artistId']} on {row['snapshotDate']}")
    return band, status


def analyze(directory, name, legacy, candidate):
    archetypes = {}
    for row in rows(directory, name, 'opening-following-census'):
        artist, group = row['artistId'], row['openingArchetype']
        if artist in archetypes and archetypes[artist] != group:
            raise ValueError('Opening cohort changed within census')
        archetypes[artist] = group

    def cohort(artist):
        return archetypes.get(artist, 'OutsideOpeningFollowingCensus')

    launches = [r for r in rows(directory, name, 'stature-marketing')
                if r['context'] == 'release-launch' and r['status'] == 'Available']
    requested_keys = {(r['snapshotDate'], r['artistId']) for r in launches}
    snapshots = {}
    priors = {}, {}
    counts, affected, transitions = defaultdict(Counter), defaultdict(set), defaultdict(Counter)
    dates, ids = [], set()
    day = None
    checked = 0
    parsed_date = lru_cache(maxsize=256)(parse_date)
    for row in rows(directory, name, 'stature-shadow'):
        stamp, artist = row['snapshotDate'], row['artistId']
        if not stamp:
            if row['status'] != 'warming_up' or dates:
                raise ValueError('Unexpected missing snapshot date')
            continue
        if stamp != day:
            if not dates and stamp != legacy['effectiveDate'] or dates and date.fromisoformat(stamp) != date.fromisoformat(day) + timedelta(days=7):
                raise ValueError('Replay weeks are not consecutive from the effective date')
            dates.append(stamp)
            day, ids = stamp, set()
        if artist in ids or parsed_date(row['date']).isoformat() != stamp:
            raise ValueError('Duplicate or mismatched snapshot')
        ids.add(artist)
        inputs = evidence(row, {field: optional(row[field]) for field in ('regionalDraw', 'recordReach', 'conversionReach')}, 'full')
        before = evaluate(inputs, legacy, stamp, priors[0].get(artist))
        after = evaluate(inputs, candidate, stamp, priors[1].get(artist))
        old_band, old_status = check_legacy(row, before, priors[0].get(artist))
        new_status = STATUS[after['status']]
        new_band = priors[1].get(artist) if new_status == 'MissingEvidence' else after['band']
        priors[0][artist], priors[1][artist] = old_band, new_band
        before_high = old_band in HIGH and old_status != 'MissingEvidence'
        after_high = new_band in HIGH and new_status != 'MissingEvidence'
        lost = before_high and not after_high
        for group in ('All', cohort(artist)):
            key = stamp[:4], group
            c = counts[key]
            c['observations'] += 1
            c['legacyHighBand'] += before_high
            c['candidateHighBand'] += after_high
            c['lostHighBand'] += lost
            c['gainedHighBand'] += after_high and not before_high
            c['legacyMissingEvidence'] += old_status == 'MissingEvidence'
            c['candidateMissingEvidence'] += new_status == 'MissingEvidence'
            if lost:
                affected[key].add(artist)
                target = new_status if new_status == 'MissingEvidence' else new_band or 'BelowRising'
                transitions[key][old_band + ' -> ' + target] += 1
        if (stamp, artist) in requested_keys:
            snapshots[stamp, artist] = (old_band, old_status, new_band, new_status)
        checked += 1
    if not checked or snapshots.keys() != requested_keys:
        raise ValueError('Missing roster observations or launch snapshot joins')

    budget_counts, budget_artists, examples = defaultdict(Counter), defaultdict(set), []
    for row in launches:
        artist = row['artistId']
        old_band, old_status, new_band, new_status = snapshots[row['snapshotDate'], artist]
        actual = float(row['proposedBudget'])
        tolerance = max(.001, actual * 2e-6)
        if old_status == 'MissingEvidence' or not math.isclose(quote(old_band or 'BelowRising', row), actual, rel_tol=0, abs_tol=tolerance):
            raise ValueError('Legacy launch proposal failed reconstruction')
        proposed = float(row['legacyBudget']) if new_status == 'MissingEvidence' else quote(new_band or 'BelowRising', row)
        delta = proposed - actual
        for group in ('All', cohort(artist)):
            key = str(parsed_date(row['date']).year), group
            c = budget_counts[key]
            c['availableLegacyReads'] += 1
            c['candidateLower'] += delta < -tolerance
            c['candidateHigher'] += delta > tolerance
            c['candidateMissingEvidence'] += new_status == 'MissingEvidence'
            if abs(delta) > tolerance:
                budget_artists[key].add(artist)
        if abs(delta) > tolerance:
            examples.append({**row, 'cohort': cohort(artist), 'candidateBand': new_band or 'BelowRising',
                             'candidateStatus': new_status, 'candidateProposedBudget': proposed, 'delta': delta})
    return dict(checkedObservations=checked, distinctArtists=len(priors[0]), firstSnapshot=dates[0], lastSnapshot=dates[-1],
        bands=[dict(year=year, cohort=group, **c, distinctArtistsLosingHighBand=len(affected[year, group]),
                    highBandLossTransitions=dict(transitions[year, group])) for (year, group), c in sorted(counts.items())],
        launchProposals=[dict(year=year, cohort=group, **c, distinctChangedArtists=len(budget_artists[year, group]))
                         for (year, group), c in sorted(budget_counts.items())],
        changedLaunches=examples)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--prefix', default='stature-geography-2y-v1')
    parser.add_argument('--geography', type=Path, default=Path('SimLogs/writing-calibration/stature-geography-2y-v1-analysis.json'))
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    legacy_path = Path('SimTools/StatureShadowExperimental.json')
    candidate_path = Path('SimTools/StatureShadowNationalHighTiers.json')
    legacy, candidate = (json.loads(p.read_text()) for p in (legacy_path, candidate_path))
    candidate_is_scoped(legacy, candidate)
    retained = json.loads(args.geography.read_text())
    if retained['calibrationSha256'] != file_digest(legacy_path):
        raise ValueError('Retained calibration changed')
    report = dict(analysisSha256=file_digest(Path(__file__)), geographySha256=file_digest(args.geography),
                  legacyCalibrationSha256=file_digest(legacy_path), candidateCalibrationSha256=file_digest(candidate_path),
                  scope='All logged AI standing evidence with fixed observed inputs and independent policy histories. '
                        'Launch proposals cover reads available under the original policy. Cohorts use the opening following census.', seeds={})
    for seed, worlds in retained['seeds'].items():
        report['seeds'][seed] = {}
        for arm, world in worlds.items():
            name = f'{args.prefix}-{seed}-{arm}'
            meta = args.directory / 'writing-calibration'
            request_path, manifest_path = (meta / f'{name}-{suffix}.json' for suffix in ('request', 'outputs'))
            if file_digest(request_path) != world['requestSha256'] or file_digest(manifest_path) != world['outputManifestSha256']:
                raise ValueError('World differs from retained geography provenance')
            request = json.loads(request_path.read_text(encoding='utf-8-sig'))
            weeks = next(int(f.split('=', 1)[1]) for f in request['effectiveArguments'] if f.startswith('--weeks='))
            if f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}' not in (meta / f'{name}.log').read_text().splitlines() or (meta / f'{name}-errors.log').stat().st_size:
                raise ValueError('Retained world did not complete cleanly')
            manifest = {item['name']: item for item in json.loads(manifest_path.read_text(encoding='utf-8-sig'))}
            for suffix in ('stature-shadow', 'stature-marketing', 'opening-following-census'):
                filename = f'{name}-{suffix}.csv'
                if csv_digest(args.directory / filename).upper() != manifest[filename]['sha256'].upper():
                    raise ValueError('Input differs from original output manifest: ' + filename)
            result = analyze(args.directory, name, policy(legacy), policy(candidate))
            if result['lastSnapshot'] != max(r['date'] for r in world['weekly']):
                raise ValueError('Incomplete roster replay')
            report['seeds'][seed][arm] = result
            print(f"Checked {result['checkedObservations']} observations in {name}", flush=True)
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')


if __name__ == '__main__':
    main()
