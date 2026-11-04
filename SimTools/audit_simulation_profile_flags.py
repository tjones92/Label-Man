"""Inventory runtime flag literals and distinguish scene defaults from profile flags.

This is a source inventory, not a claim that every absent literal disables a
feature. Owners can enable behavior through scene exports, implied dependencies,
nonzero defaults, or saved state. Reviewed interpretations live in the report.
"""
import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CAL = ROOT / 'SimLogs' / 'writing-calibration'


def inventory():
    locations = defaultdict(set)
    hashes = {}
    for folder in ('Systems', 'Data'):
        for path in sorted((ROOT / folder).rglob('*.cs')):
            raw = path.read_bytes()
            relative = path.relative_to(ROOT).as_posix()
            hashes[relative] = hashlib.sha256(raw).hexdigest()
            # Strip line comments before inspecting string literals. This remains
            # a conservative inventory; interpolation and composite names need owner review.
            source = re.sub(r'//[^\n]*', '', raw.decode('utf-8-sig'))
            for flag in re.findall(r'"(--[a-z][a-z0-9-]*(?:=[^"\r\n]*)?)"', source):
                locations[flag.split('=')[0]].add(relative)
            if relative == 'Systems/RegionalAudience.cs':
                for name in re.findall(r'Switch\(args, "([a-z-]+)"', source):
                    for prefix in ('--', '--no-'):
                        locations[prefix + name].add(relative)
    profile_source = (ROOT / 'Systems/SimulationArguments.cs').read_text(encoding='utf-8-sig')
    block = profile_source.split('string[] Profile = {', 1)[1].split('};', 1)[0]
    profile = re.findall(r'"(--[^"\r\n]+)"', block)
    manifest = json.loads((CAL / 'writing-full-profile-market-1001-manifest.json').read_text(encoding='utf-8-sig'))
    explicit = manifest['flags']
    explicit_keys = {f.split('=')[0] for f in explicit}
    effective = [f for f in profile if f.split('=')[0] not in explicit_keys] + explicit
    scene = (ROOT / 'chart_manager.tscn').read_text(encoding='utf-8-sig')
    chart = (ROOT / 'Systems/ChartManager.cs').read_text(encoding='utf-8-sig')
    defaults = dict(re.findall(r'\[Export\] private bool (\w+) = (true|false);', chart))
    overrides = dict(re.findall(r'^(\w+) = (true|false)$', scene, re.MULTILINE))
    defaults.update({k: v for k, v in overrides.items() if k in defaults})
    for name in ('chart_manager.tscn', 'project.godot'):
        hashes[name] = hashlib.sha256((ROOT / name).read_bytes()).hexdigest()
    working = json.loads((ROOT / 'SimLogs/working-bundle/plan.json').read_text(encoding='utf-8-sig'))
    return dict(profile=profile, investigationExplicitFlags=explicit,
                investigationResolvedFlags=effective,
                archivedWorkingBundleMissingFromOrdinaryProfile=[f for f in working['bundle']
                                                                 if f not in profile],
                chartManagerSceneBooleans={k: v == 'true' for k, v in sorted(defaults.items())},
                runtimeFlagKeys=[dict(flag=k, inOrdinaryProfile=k in {f.split('=')[0] for f in profile},
                                      explicitInInvestigation=k in explicit_keys, owners=sorted(v))
                                 for k, v in sorted(locations.items())],
                currentSourceSha256=hashes,
                caveat='Source inventory only; absent flag is not equivalent to disabled behavior. Saved games retain some settings.')


if __name__ == '__main__':
    result = inventory()
    path = CAL / 'writing-profile-flag-inventory.json'
    path.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(runtimeFlagKeys=len(result['runtimeFlagKeys']),
                          ordinaryProfileFlags=len(result['profile']),
                          investigationResolvedFlags=len(result['investigationResolvedFlags']),
                          chartManagerSceneBooleans=result['chartManagerSceneBooleans']), indent=2))
