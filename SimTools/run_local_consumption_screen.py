"""Fresh, live experiments. No replay. Immutable per-run args, hashes and completion receipts."""
import argparse, concurrent.futures, hashlib, json, os, pathlib, shutil, subprocess, time

ROOT = pathlib.Path(__file__).resolve().parents[1]
GODOT = pathlib.Path('C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
DLL = ROOT / '.godot/mono/temp/bin/Debug/Label Man.dll'
BASE = '''--station-regional-support=standing --station-regional-support-rank-noise=0.5
--station-regional-support-unsold-floor --station-regional-support-panel-weight=0.085
--regional-fit-exposure --regional-fit-exposure-relive --regional-fit-exposure-relive-awareness'''.split()
PICKS = '''--station-picks=2 --station-picks-score=fit --station-picks-age=2 --station-picks-fit-noise=0.5
--station-picks-fit-bar=2.4 --station-picks-fit-unserviced'''.split()
ARMS = {
    'ctl': [], 'pp': [], 'nopicks': [],
    'contact1': ['--local-consumption-contact=1'],
    'contact3': ['--local-consumption-contact=3'],
    'familiar2': ['--local-consumption-familiarity=2'],
    'familiar6': ['--local-consumption-familiarity=6'],
    'peer2': ['--local-consumption-peer=2'],
    'peer8': ['--local-consumption-peer=8'],
    'shelf': ['--local-consumption-shelf=1'],
    'signal': ['--local-consumption-radio-signal'],
    'nofloors': ['--probe-sync-no-floors'],
    'localonly': ['--probe-sync-regional-awareness-only'],
    'combo': ['--local-consumption-contact=1','--local-consumption-familiarity=2','--local-consumption-peer=2'],
    'listener3': ['--local-consumption-contact=3','--local-consumption-listeners'],
    'audition_random': ['--response-auditions=random'],
    'audition_response': ['--response-auditions=response'],
    'audience_random': ['--response-auditions=random', '--bounded-audience=radio'],
    'audience_response': ['--response-auditions=response', '--bounded-audience=radio'],
    'sharing_random': ['--response-auditions=random', '--bounded-audience=sharing'],
    'sharing_response': ['--response-auditions=response', '--bounded-audience=sharing'],
    'lre': ['--local-retail-exposure'],
    'nb': ['--regional-fit-exposure-launch-base=0.1'],
    'lre_nb': ['--local-retail-exposure', '--regional-fit-exposure-launch-base=0.1'],
    'lre05': ['--local-retail-exposure=0.5'],
    'lre03': ['--local-retail-exposure=0.3'],
    'lre05_nb': ['--local-retail-exposure=0.5', '--regional-fit-exposure-launch-base=0.1'],
    'lre04': ['--local-retail-exposure=0.4'],
    # Per-channel λ: rack access, top-20 shelf and restocks on the national chart; +jukebox puts the exposure weight there too.
    'lre04t': ['--local-retail-exposure=0.4', '--local-retail-exposure-trade=0'],
    'lre04tj': ['--local-retail-exposure=0.4', '--local-retail-exposure-trade=0', '--local-retail-exposure-trade-jukebox'],
    'lre05t05': ['--local-retail-exposure=0.5', '--local-retail-exposure-trade=0.5'],
}

def main():
    p = argparse.ArgumentParser()
    p.add_argument('--tag', required=True)
    p.add_argument('--weeks', type=int, default=12)
    p.add_argument('--seeds', default='1001')
    p.add_argument('--arms', default=','.join(ARMS))
    p.add_argument('--jobs', type=int, default=3)
    p.add_argument('--observe', action='store_true')
    p.add_argument('--extra', action='append', default=[])
    a = p.parse_args()
    out = ROOT / 'SimLogs' / a.tag
    out.mkdir(exist_ok=False)
    assembly = hashlib.sha256(DLL.read_bytes()).hexdigest()
    shutil.copy2(DLL,out/'experiment-assembly.dll')
    for source in ['Systems/LocalRadioConsumptionProbe.cs','Systems/ChartSimulator.cs','Systems/ChartManager.cs',
                   'Systems/BoundedAudienceState.cs','Systems/BoundedAudienceProbe.cs','Systems/ArtistFanbase.cs',
                   'Systems/Radio/StationNetwork.Aggregation.cs','Systems/Radio/ResponseAuditionProbe.cs',
                   'Systems/Radio/StationNetwork.Playlist.cs','Systems/Radio/PlaylistMeeting.cs',
                   'Systems/Radio/StationPicks.cs','Systems/SingleCommercialFit.cs','Systems/RegionalFitExposure.cs','Systems/LocalRetailExposure.cs',
                   'SimTools/run_local_consumption_screen.py']:
        target=out/'source'/source
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/source,target)
    profile = (ROOT / 'SimTools/regional_fit_profile_flags.txt').read_text().split()
    profile = [f for f in profile if not f.startswith(('--single-commercial-fit-launch-weight=', '--slate-work-budget-midtier='))]
    profile += ['--single-commercial-fit-launch-weight=0.25', '--slate-work-budget-midtier=2.5']
    common = '''--small-label-finance --profile-performance --historical-independent-stars
--slate-work-budget-named-independents=2.5 --legacy-simulation-profile --artist-written-hook-scale
--self-writing-craft-gate=0.5 --rock-hit-writers --rock-hit-office-placement=2 --rock-hit-office-opening
--station-playlists'''.split() + profile + BASE
    runs = []
    for seed in map(int,a.seeds.split(',')):
        for arm in a.arms.split(','):
            flags = common + ([] if arm == 'nopicks' else PICKS)
            if arm not in ('pp','nopicks'): flags += ['--station-picks-tastemaker-indie']
            flags += ARMS[arm] + a.extra
            if a.observe: flags += ['--local-consumption-observe']
            name = f'{a.tag}-{arm}-{seed}'
            cmd = [str(GODOT), '--headless', '--path', str(ROOT), '--log-file', str(out / f'{name}-engine.log'),
                   'SimTools/ChartAuditRunner.tscn', '--', f'--run={name}', f'--weeks={a.weeks}', f'--seed={seed}'] + flags
            runs.append(dict(name=name, arm=arm, seed=seed, weeks=a.weeks, args=cmd, assembly=assembly))
    (out / 'plan.json').write_text(json.dumps(runs,indent=2))
    env = os.environ | {'DOTNET_gcServer':'1','DOTNET_GCHeapCount':'4'}
    def run(r):
        start = time.monotonic()
        log = out / (r['name']+'-console.log')
        with log.open('w') as f:
            result = subprocess.run(r['args'],cwd=ROOT,env=env,stdout=f,stderr=subprocess.STDOUT,
                                    creationflags=subprocess.CREATE_NO_WINDOW)
        receipt = r | dict(seconds=time.monotonic()-start,exitCode=result.returncode,
                           complete=f"CHART_AUDIT_COMPLETE run={r['name']} weeks={a.weeks}" in log.read_text(errors='replace'),
                           assemblyAfter=hashlib.sha256(DLL.read_bytes()).hexdigest())
        (out / (r['name']+'-receipt.json')).write_text(json.dumps(receipt,indent=2))
        print(json.dumps({k:receipt[k] for k in ('name','seconds','exitCode','complete')}),flush=True)
        if not receipt['complete'] or result.returncode or receipt['assemblyAfter'] != assembly:
            raise RuntimeError(f"Failed run {r['name']}; inspect receipt/log")
        return receipt
    with concurrent.futures.ThreadPoolExecutor(max_workers=a.jobs) as pool:
        receipts = list(pool.map(run,runs))
    (out / 'runs.json').write_text(json.dumps(receipts,indent=2))

if __name__ == '__main__': main()
