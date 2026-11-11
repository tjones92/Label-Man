"""Require all original CSV hashes to match the existing opening-origin smoke pair."""
import argparse
import json
from pathlib import Path

from compress_stature_outputs import csv_digest

EXTRA = {'opening-geography-releases.csv', 'opening-geography-regions.csv'}


def verify(directory, prefix, baseline, seed):
    report = {}
    for arm in ('control', 'treatment'):
        manifests, requests = [], []
        for run_prefix in (baseline, prefix):
            name = f'{run_prefix}-{seed}-{arm}'
            meta = directory / 'writing-calibration'
            manifest = json.loads((meta / f'{name}-outputs.json').read_text(encoding='utf-8-sig'))
            requests.append(json.loads((meta / f'{name}-request.json').read_text(encoding='utf-8-sig')))
            for entry in manifest:
                if csv_digest(directory / entry['name']).upper() != entry['sha256'].upper():
                    raise ValueError('Output no longer matches manifest: ' + entry['name'])
            manifests.append({r['name'][len(name) + 1:]: r['sha256'].upper() for r in manifest})
        old, new = manifests
        if set(new) - set(old) != EXTRA or set(old) - set(new):
            raise ValueError('Unexpected CSV differences: ' + arm)
        differs = [suffix for suffix in old if old[suffix] != new[suffix]]
        if differs:
            raise ValueError('Geography observation differs from baseline: ' + arm + ': ' + ', '.join(differs))
        def economic_flags(request):
            return [f for f in request['effectiveArguments'] if not f.startswith('--run=') and f != '--observe-opening-geography']
        if economic_flags(requests[0]) != economic_flags(requests[1]):
            raise ValueError('Effective arguments differ beyond run name and geography observation')
        if requests[0]['engineSha256'] != requests[1]['engineSha256'] or requests[0]['calibrationSha256'] != requests[1]['calibrationSha256']:
            raise ValueError('Engine/calibration differs')
        report[arm] = {'byteIdenticalOriginalCsvs': len(old), 'newGeographyCsvs': len(EXTRA)}
    return report


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--prefix', default='stature-geography-smoke-v1')
    p.add_argument('--baseline', default='stature-origin-smoke-v1')
    p.add_argument('--seed', type=int, default=1001)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    report = verify(a.directory, a.prefix, a.baseline, a.seed)
    with a.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2); stream.write('\n')
    print(json.dumps(report))
