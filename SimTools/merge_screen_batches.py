"""Merge completed screen batches into one analysis folder (plan.json plus receipts), so the analyzers read them as one
screen. Runs keep their own names and SimLogs outputs. A run present in several batches is taken from the first one
listed; byte-compare duplicates first (they must come from the same assembly)."""
import argparse, json, pathlib, shutil

ROOT = pathlib.Path(__file__).resolve().parents[1]


def main():
    p = argparse.ArgumentParser()
    p.add_argument('tag', help='new folder under SimLogs')
    p.add_argument('sources', nargs='+', help='batch folders under SimLogs, in order of preference')
    p.add_argument('--arms', help='comma-separated arms to keep (default: all)')
    a = p.parse_args()
    keep = set(a.arms.split(',')) if a.arms else None
    out = ROOT / 'SimLogs' / a.tag
    out.mkdir(exist_ok=False)
    plan, seen, assemblies = [], set(), set()
    for source in a.sources:
        folder = ROOT / 'SimLogs' / source
        for run in json.loads((folder / 'plan.json').read_text()):
            receipt = folder / (run['name'] + '-receipt.json')
            if (keep and run['arm'] not in keep) or (run['arm'], run['seed']) in seen or not receipt.exists():
                continue
            rc = json.loads(receipt.read_text())
            if not rc['complete'] or rc['exitCode'] or rc['assembly'] != rc['assemblyAfter']:
                continue
            seen.add((run['arm'], run['seed']))
            assemblies.add(rc['assembly'])
            plan.append(run)
            shutil.copy2(receipt, out / receipt.name)
    if len(assemblies) != 1:
        raise ValueError(f'batches span {len(assemblies)} assemblies')
    (out / 'plan.json').write_text(json.dumps(plan, indent=2))
    for arm in sorted({r['arm'] for r in plan}):
        print(arm, sorted(r['seed'] for r in plan if r['arm'] == arm))


if __name__ == '__main__':
    main()
