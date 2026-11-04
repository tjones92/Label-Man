"""Compare the frozen writing-market controls with the causal media/fanbase bundle."""
import json
from pathlib import Path
from analyze_writing_workflow import summarize

ROOT = Path(__file__).resolve().parents[1]
CAL = ROOT / 'SimLogs' / 'writing-calibration'
ADDED = {'--causal-media-atmosphere', '--media-recognition', '--fame-registration',
         '--observe-following', '--media-residencies', '--follower-demand'}

def manifest(run):
    return json.loads((CAL / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))

def flags(m):
    return {f for f in m['flags'] if not f.startswith('--run=')}

result=[]
for seed in (1001, 2002, 3003):
    control=f'plan-pilot-control-market-{seed}'
    treatment=f'writing-fanbase-market-{seed}'
    cm, tm=manifest(control), manifest(treatment)
    if cm['assemblySha256'] != tm['assemblySha256'] or flags(tm) != flags(cm) | ADDED:
        raise ValueError(f'{seed}: assembly or flags differ beyond the fanbase bundle')
    if flags(cm) & ADDED: raise ValueError(f'{seed}: control already has fanbase flags')
    result.append({'seed':seed, 'control':summarize(control,104), 'fanbase':summarize(treatment,104)})
(CAL/'writing-fanbase-comparison.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
full_run = 'writing-full-profile-market-1001'
full = manifest(full_run)
fan = manifest('writing-fanbase-market-1001')
if full['assemblySha256'] != fan['assemblySha256'] or flags(full) != flags(fan) | {'--default-simulation-profile'}:
    raise ValueError('Full-profile comparison has unexpected flag or assembly differences')
full_result = summarize(full_run,104)
(CAL/'writing-full-profile-exploratory.json').write_text(json.dumps(full_result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'paired':result, 'fullProfileSeed1001':full_result},indent=2))
