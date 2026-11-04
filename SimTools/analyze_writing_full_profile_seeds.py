"""Compare matched causal-fanbase worlds with and without the ordinary profile."""
import json
from pathlib import Path

from analyze_material_funnel import summarize as funnel
from analyze_writing_workflow import summarize

CAL = Path(__file__).resolve().parents[1] / 'SimLogs' / 'writing-calibration'
HASH = '511E9709FE20879CE4636C7FFE39F9C62E9CE825DC8EB01A110041711D221AC5'


def manifest(run):
    return json.loads((CAL / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))


def flags(m):
    return {f for f in m['flags'] if not f.startswith('--run=')}


results = []
for seed in (1001, 2002, 3003):
    control = f'writing-fanbase-market-{seed}'
    full = (f'writing-full-profile-market-{seed}' if seed == 1001 else
            f'writing-full-profile-extension-market-{seed}')
    cm, fm = manifest(control), manifest(full)
    if cm['assemblySha256'] != HASH or fm['assemblySha256'] != HASH:
        raise ValueError(f'{seed}: unexpected assembly')
    if '--default-simulation-profile' in flags(cm) or flags(fm) != flags(cm) | {'--default-simulation-profile'}:
        raise ValueError(f'{seed}: unexpected flag differences')
    results.append({'seed': seed, 'fanbase': summarize(control, 104),
                    'fullProfile': summarize(full, 104),
                    'fanbaseFunnel': funnel(control, 104), 'fullProfileFunnel': funnel(full, 104)})
(CAL / 'writing-full-profile-seed-comparison.json').write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
print(json.dumps(results, indent=2))
