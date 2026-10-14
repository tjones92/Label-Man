"""Offline Phase 0 census and byte-level control comparisons; never starts Godot."""
import argparse
import csv
import gzip
import hashlib
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
COHORTS = {0: 'InitialLegacy', 1: 'EnabledInitialReserve', 2: 'RuntimeFormation'}
SEARCH = {0: 'NotProspect', 1: 'Latent', 2: 'Seeking'}
GENRES = {int(value): name for name, value in re.findall(r'\b(\w+)\s*=\s*(\d+)', (ROOT / 'Data' / 'Genre.cs').read_text())}


def census(path):
    with gzip.open(path, 'rt', encoding='utf-8') as stream:
        envelope = json.load(stream)
    world = envelope['World']
    artists = world['Artists']
    by_id = {a['artistId']: a for a in artists}
    unsigned = world['UnsignedArtistIds']
    violations = []
    if len(by_id) != len(artists):
        violations.append('duplicate artist IDs')
    if len(set(unsigned)) != len(unsigned):
        violations.append('duplicate unsigned IDs')
    for artist_id in unsigned:
        if artist_id not in by_id:
            violations.append(f'dangling unsigned artist: {artist_id}')
        elif by_id[artist_id].get('labelId'):
            violations.append(f'owned artist in unsigned pool: {artist_id}')
    roster_owners = defaultdict(list)
    for label in world['Labels']:
        owner = label['Label']['labelId']
        for artist_id in label['RosterArtistIds']:
            roster_owners[artist_id].append(owner)
            if artist_id not in by_id:
                violations.append(f'dangling roster artist: {owner}/{artist_id}')
            elif by_id[artist_id].get('labelId') != owner:
                violations.append(f'roster ownership mismatch: {owner}/{artist_id}')
    for artist_id, owners in roster_owners.items():
        if len(owners) != 1:
            violations.append(f'duplicate roster ownership: {artist_id}/{owners}')
    active_memberships = defaultdict(list)
    all_person_ids = set()
    for artist in artists:
        for member in artist.get('members', []):
            person_id = member['personId']
            all_person_ids.add(person_id)
            if member.get('isActive') and artist.get('isActive'):
                active_memberships[person_id].append(artist['artistId'])
    pooled = (world.get('BandLife') or {}).get('Pool', [])
    pool_ids = [p['person']['personId'] for p in pooled]
    all_person_ids.update(pool_ids)
    if len(set(pool_ids)) != len(pool_ids):
        violations.append('duplicate person-pool IDs')
    for person_id, acts in active_memberships.items():
        if len(acts) != 1:
            violations.append(f'multiple active act memberships: {person_id}/{acts}')
        if person_id in set(pool_ids):
            violations.append(f'person both pooled and actively assigned: {person_id}')
    for artist in artists:
        owner = artist.get('labelId')
        if owner and artist['artistId'] not in roster_owners:
            violations.append(f'owned artist absent from owner roster: {artist["artistId"]}/{owner}')
    dimensions = Counter((a.get('homeRegion', 'unknown'), GENRES.get(a.get('primaryGenre'), str(a.get('primaryGenre'))), COHORTS.get(a.get('cohort'), str(a.get('cohort'))),
                          SEARCH.get(a.get('prospectMarketStatus'), str(a.get('prospectMarketStatus'))),
                          'owned' if a.get('labelId') else 'unowned', bool(a.get('isActive'))) for a in artists)
    canonical = json.dumps(world, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()
    return {
        'snapshot': path.name, 'seed': envelope.get('WorldSeed'), 'chartWeek': world['ChartWeek'],
        'date': [envelope['Year'], envelope['Month'], envelope['Day']], 'flags': envelope['Flags'],
        'registryActs': len(artists), 'activeActs': sum(bool(a.get('isActive')) for a in artists),
        'unsignedShelf': len(unsigned), 'ownedActs': sum(bool(a.get('labelId')) for a in artists),
        'uniquePeopleInSavedActsAndPool': len(all_person_ids), 'activeAssignedPeople': len(active_memberships),
        'pooledPeople': len(pool_ids), 'labels': len(world['Labels']),
        'labelAssignmentSources': dict(Counter(label['Label'].get('homeCityAssignmentSource', 'missing') for label in world['Labels'])),
        'labelGeography': [{key: label['Label'].get(key) for key in ('labelId', 'headquartersCity', 'homeRegion', 'homeCityId', 'homeCityAssignmentSource')} for label in world['Labels']],
        'formationDebt': (world.get('BandLife') or {}).get('FormationDebt'),
        'formationCredit': (world.get('BandLife') or {}).get('FormationCredit'),
        'worldCanonicalSha256': hashlib.sha256(canonical).hexdigest(),
        'violations': violations,
        'groups': [dict(zip(('region', 'genre', 'cohort', 'search', 'ownership', 'active', 'count'), (*key, n)))
                   for key, n in sorted(dimensions.items())],
        'scope': 'Authoritative captured world acts and their saved members/pool; no player label was founded. Not a census of unpublished musician-registry orphans.'
    }


def compare(first, second):
    def files(run):
        return {p.name[len(run) + 1:]: p for p in (ROOT / 'SimLogs').glob(run + '-*.csv')
                if not p.name.endswith('-performance.csv')}
    left, right = files(first), files(second)
    differences = []
    hashes = {}
    for suffix in sorted(left.keys() | right.keys()):
        if suffix not in left or suffix not in right:
            differences.append(suffix + ': missing file')
            continue
        a = hashlib.sha256(left[suffix].read_bytes()).hexdigest()
        b = hashlib.sha256(right[suffix].read_bytes()).hexdigest()
        hashes[suffix] = {'first': a, 'second': b}
        if a != b:
            differences.append(suffix)
    if not hashes:
        differences.append('no comparable CSVs')
    return {'first': first, 'second': second, 'comparedFiles': len(hashes), 'differences': differences,
            'hashes': hashes, 'excluded': ['performance timing only']}


def metrics(run):
    def rows(suffix):
        with (ROOT / 'SimLogs' / f'{run}-{suffix}.csv').open(encoding='utf-8-sig', newline='') as stream:
            return list(csv.DictReader(stream))
    labor = rows('artist-labor-market-weekly')
    population = rows('artist-population-weekly')
    economy = rows('decade-annual-rollup')
    finance = rows('label-finance')
    last_week = max(int(row['week']) for row in finance)
    last_finance = [row for row in finance if int(row['week']) == last_week]
    integrity_columns = ('ownershipConflicts', 'duplicateRosterEntries', 'duplicatePoolEntries',
                         'terminalRostered', 'terminalReleaseEligible')
    labor_columns = ('duplicateSeekingEntries', 'latentUnsignedPoolEntries', 'seekingMissingFromUnsignedPool',
                     'prospectStatusContractConflicts')
    integrity = {key: max(int(row[key]) for row in population) for key in integrity_columns}
    integrity.update({key: max(int(row[key]) for row in labor) for key in labor_columns})
    annual = economy[-1]
    return {'run': run, 'lastLaborMarket': labor[-1], 'maxIntegrityCounts': integrity,
            'lastWeekLabels': len(last_finance), 'lastWeekLabelStatuses': dict(Counter(row['status'] for row in last_finance)),
            'singleUnits': int(annual['singleUnits']), 'albumUnits': int(annual['albumUnits']),
            'singleGross': float(annual['singleGross']), 'albumGross': float(annual['albumGross']),
            'singleNet': float(annual['singleNet']), 'albumNet': float(annual['albumNet'])}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('run_tag')
    args = parser.parse_args()
    if not args.run_tag or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789-' for c in args.run_tag):
        parser.error('invalid run tag')
    tag = args.run_tag
    out = ROOT / 'SimLogs' / tag
    report = {'censuses': [census(ROOT / 'SimLogs' / 'worlds' / f'{tag}-w0-{seed}.world.json.gz')
                          for seed in ('1001', 'repeat-1001', '2002')],
              'comparisons': [compare(f'{tag}-1001', f'{tag}-repeat-1001'),
                              compare(f'{tag}-replay-a-1001', f'{tag}-replay-b-1001')]}
    report['metrics'] = [metrics(f'{tag}-{seed}') for seed in ('1001', '2002')]
    report['repeatedWorldMatches'] = report['censuses'][0]['worldCanonicalSha256'] == report['censuses'][1]['worldCanonicalSha256']
    manifest = json.loads((out / 'runs.json').read_text(encoding='utf-8-sig'))
    report['runsComplete'] = len(manifest['results']) == len(manifest['jobs']) and all(r['exitCode'] == 0 and r['completed'] and r['weeks'] == j['weeks'] for r, j in zip(manifest['results'], manifest['jobs']))
    (out / 'analysis.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    with (out / 'census.csv').open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=('snapshot', 'region', 'genre', 'cohort', 'search', 'ownership', 'active', 'count'))
        writer.writeheader()
        for c in report['censuses']:
            for group in c['groups']:
                writer.writerow({'snapshot': c['snapshot'], **group})
    with (out / 'label-geography.csv').open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=('snapshot', 'labelId', 'headquartersCity', 'homeRegion', 'homeCityId', 'homeCityAssignmentSource'))
        writer.writeheader()
        for c in report['censuses']:
            for label in c['labelGeography']:
                writer.writerow({'snapshot': c['snapshot'], **label})
    for c in report['censuses']:
        print(f'{c["snapshot"]}: acts={c["registryActs"]}, active={c["activeActs"]}, unsigned={c["unsignedShelf"]}, '
              f'people={c["uniquePeopleInSavedActsAndPool"]}, violations={len(c["violations"])}')
    for comparison in report['comparisons']:
        print(f'{comparison["first"]} vs {comparison["second"]}: files={comparison["comparedFiles"]}, '
              f'differences={comparison["differences"]}')
    if not report['runsComplete'] or not report['repeatedWorldMatches'] or any(any(m['maxIntegrityCounts'].values()) for m in report['metrics']) or any(c['violations'] for c in report['censuses']) or any(c['differences'] for c in report['comparisons']):
        raise SystemExit(1)


if __name__ == '__main__':
    main()
