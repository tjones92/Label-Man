"""Verify and fingerprint a completed ChartAuditRunner baseline.

This records evidence, not a claim that an assembly was built from a revision.
Run names are stripped from output names so manifests can later be compared.
"""
import argparse
import hashlib
import json
from pathlib import Path


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest().upper()


def verify(request_path, assembly, log, errors, output_dir, combined_flags):
    request = json.loads(request_path.read_text(encoding='utf-8-sig'))
    flags = request['flags']
    def one(prefix):
        values = [x[len(prefix):] for x in flags if x.startswith(prefix)]
        if len(values) != 1 or not values[0]:
            raise ValueError(f'Expected exactly one {prefix} flag')
        return values[0]
    name, seed, weeks = one('--run='), one('--seed='), one('--weeks=')
    if Path(name).name != name or name in ('.', '..'):
        raise ValueError('Invalid run name')
    expected = request['assemblySha256'].upper()
    actual = digest(assembly)
    if actual != expected:
        raise ValueError('Assembly hash does not match request')
    lines = log.read_text(encoding='utf-8-sig', errors='replace').splitlines()
    marker = f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}'
    if marker not in lines:
        raise ValueError('Missing exact completion marker')
    if errors.stat().st_size:
        raise ValueError('Error log is not empty')
    required = [line.strip() for line in combined_flags.read_text(encoding='utf-8-sig').splitlines()
                if line.strip() and not line.lstrip().startswith('#')]
    missing = [flag for flag in required if flag not in flags]
    if missing:
        raise ValueError('Missing combined flags: ' + ', '.join(missing))
    outputs = sorted(output_dir.glob(name + '-*.csv'))
    if not outputs or not any(p.name == name + '-records.csv' for p in outputs):
        raise ValueError('Missing records or other CSV outputs')
    return {'run': name, 'seed': seed, 'weeks': weeks, 'flags': flags,
            'effectiveArguments': request.get('effectiveArguments'),
            'assemblySha256': actual, 'sourceRevision': request.get('sourceRevision'),
            'sourceProvenance': ('recorded by run request; not independently verified' if request.get('sourceRevision')
                                 else 'not established by this verifier'),
            'requestSha256': digest(request_path), 'logSha256': digest(log),
            'errorsSha256': digest(errors), 'combinedFlagsSha256': digest(combined_flags),
            'outputs': {p.name[len(name) + 1:]: {'bytes': p.stat().st_size, 'sha256': digest(p)}
                        for p in outputs}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for option in ('request', 'assembly', 'log', 'errors', 'output-dir', 'combined-flags', 'manifest'):
        parser.add_argument('--' + option, required=True, type=Path)
    args = parser.parse_args()
    result = verify(args.request, args.assembly, args.log, args.errors,
                    args.output_dir, args.combined_flags)
    # Exclusive creation protects the frozen manifest from accidental replacement.
    with args.manifest.open('x', encoding='utf-8', newline='\n') as target:
        json.dump(result, target, indent=2, sort_keys=True)
        target.write('\n')
    print(f"Verified {result['run']}: {len(result['outputs'])} CSV outputs; source provenance unverified")


if __name__ == '__main__':
    main()
