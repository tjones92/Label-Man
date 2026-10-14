from pathlib import Path
import hashlib,json,sys
root=Path(__file__).resolve().parents[1]
if root.name=='Label-Man' and (root/'Systems').is_dir():
    pass
else:
    root=Path(r'C:\Users\grohl\.codex\worktrees\local-scene-phase-6\Label-Man')
prior=Path(sys.argv[2]) if len(sys.argv)>2 else Path(r'C:\Users\grohl\.codex\worktrees\local-scene-phase-5-feedback\Label-Man')
tag=sys.argv[1] if len(sys.argv)>1 else 'scene6-final-v2'
def files(base,mode):
    prefix=f'{tag}-{mode}-1001-'
    return {p.name[len(prefix):]:hashlib.sha256(p.read_bytes()).hexdigest() for p in (base/'SimLogs').glob(prefix+'*.csv') if not p.name[len(prefix):].startswith('scene-')}
control=files(prior,'control')
assert len(control)==84,len(control)
report={'weeks':8,'seed':1001,'control':'Phase 5 with institutions, relocation and price explicitly enabled','comparisons':[]}
for mode in ('default','explicit'):
    other=files(root,mode)
    differences=[k for k in sorted(control.keys()|other.keys()) if control.get(k)!=other.get(k)]
    report['comparisons'].append({'mode':mode,'identicalEconomicFiles':len(control)-len(differences),'differences':differences})
    assert not differences,report
manifest=json.loads((root/f'SimLogs/{tag}/runs.json').read_text(encoding='utf-8-sig'))
assert len(manifest['results'])==7 and all(r['passed'] for r in manifest['results'])
report['passed']=True
report['decadeValidationDeferred']=True
(root/f'SimLogs/{tag}/analysis.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
