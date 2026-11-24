"""Isolate RegionalName source sensitivity and bound opening geography inputs.

Footprints are hypothetical, not reconstructed release territories. Each keeps
total stock fixed and weights it by population inside a selected footprint.
"""
import argparse
import itertools
import json
import math
import re
from collections import defaultdict
from pathlib import Path

from partition_opening_following_routes import analyze, policy
from stature_normalization import file_digest
from trace_stature_marketing_mechanisms import rows


def populations(scene):
    result = {}
    for block in scene.read_text(encoding='utf-8').split('[sub_resource '):
        region = re.search(r'^regionId = "([^"]+)"$', block, re.M)
        pop = re.search(r'^population = ([0-9.]+)$', block, re.M)
        if region and pop:
            result[region[1]] = float(pop[1])
    if len(result) != 7 or any(p <= 0 for p in result.values()):
        raise ValueError('Expected seven positive regional populations')
    return result


def inputs(stock, population, footprint, reference):
    if stock < 0 or reference <= 0 or not footprint or not set(footprint) <= population.keys():
        raise ValueError('Invalid stock, reference or footprint')
    covered = sum(population[r] for r in footprint)
    density = min(1, stock / covered / reference)
    return {'regionalDraw': density,
            'conversionReach': density * covered / sum(population.values()),
            'coveredPopulationShare': covered / sum(population.values())}


def opening(census, population, reference):
    acts = defaultdict(list)
    total = 0
    for row in census:
        total += float(row['seedYoung']) + float(row['seedAdult'])
        if row['openingArchetype'] == 'RegionalName':
            acts[row['artistId']].append(row)
    result = []
    for artist, entries in sorted(acts.items()):
        stock = sum(float(r['seedYoung']) + float(r['seedAdult']) for r in entries)
        if len(entries) != len(population) or {r['region'] for r in entries} != population.keys():
            raise ValueError('RegionalName census does not span all seven regions')
        for row in entries:
            actual = float(row['seedYoung']) + float(row['seedAdult'])
            expected = stock * population[row['region']] / sum(population.values())
            if not math.isclose(actual, expected, rel_tol=2e-6, abs_tol=.01):
                raise ValueError('Opening distribution differs from population weights')
        base = inputs(stock, population, tuple(population), reference)
        scenarios = []
        for count in (1, 2, 3):
            candidates = [inputs(stock, population, f, reference)
                          for f in itertools.combinations(population, count)]
            scenarios.append({'markets': count, 'footprints': len(candidates),
                **{k + bound: operation(c[k] for c in candidates)
                   for k in ('regionalDraw', 'conversionReach')
                   for bound, operation in (('Min', min), ('Max', max))}})
        result.append({'artistId': artist, 'artist': entries[0]['artist'], 'stock': stock,
                       'actualOpeningInputs': base, 'hypotheticalFootprints': scenarios})
    regional = sum(a['stock'] for a in result)
    return {'acts': len(acts), 'stock': regional, 'allSeededStock': total,
            'shareOfSeededStock': regional / total, 'populationWeightedCensusVerified': True,
            'details': result}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--prefix', default='stature-origin-2y-v1')
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    config = Path('SimTools/StatureShadowExperimental.json')
    scene = Path('chart_manager.tscn')
    calibration = json.loads(config.read_text())
    report = {'analysisSha256': file_digest(Path(__file__)),
        'routeAnalysisSha256': file_digest(Path('SimTools/partition_opening_following_routes.py')),
        'calibrationSha256': file_digest(config), 'sceneSha256': file_digest(scene),
        'scope': 'RegionalName acts only. Removal uses existing source diagnostics and independent hysteresis with observed references. Footprint ranges are opening source-only inputs, keeping total people fixed; all possible 1/2/3-region sets, without known home constraints. They are not release-level allocations, standing-band predictions, or economic effects. Record reach also depends on sales response and is not estimated by the footprint calculation.',
        'seeds': {}}
    for seed in (1001, 2002):
        worlds = {}
        census_by_arm = []
        for arm in ('control', 'treatment'):
            name = f'{a.prefix}-{seed}-{arm}'
            census = list(rows(a.directory, name, 'opening-following-census'))
            census_by_arm.append(census)
            initial = opening(census, populations(scene), calibration['FollowingPerMillionReference'])
            selected = {r['artistId'] for r in initial['details']}
            worlds[arm] = {'opening': initial, 'sourceRemoval': analyze(a.directory, name, policy(calibration), selected)}
        if census_by_arm[0] != census_by_arm[1]:
            raise ValueError('Paired opening censuses differ')
        report['seeds'][str(seed)] = worlds
    with a.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2); stream.write('\n')


if __name__ == '__main__':
    main()
