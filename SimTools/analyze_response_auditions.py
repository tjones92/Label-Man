"""Validate and summarize live response-audition research traces, without future inputs."""
import argparse, collections, json, pathlib
import pandas as pd

ROOT = pathlib.Path(__file__).resolve().parents[1]

def analyze(run):
    path = ROOT / 'SimLogs' / (run['name'] + '-response-auditions.jsonl')
    out = {k: run[k] for k in ('name', 'arm', 'seed', 'weeks')}
    if not path.exists():
        return out
    starts, graduated, rejected = {}, set(), set()
    active_seats, offers = collections.Counter(), collections.Counter()
    sender_calls = collections.Counter()
    heard, geographic_starts, distant_starts = set(), set(), set()
    below, report_count, accepted_count = set(), 0, 0
    previous_rows = {}
    ordinary_weeks = collections.defaultdict(set)
    silent = set()
    capacity_rejected, response_rejected = set(), set()
    with path.open() as stream:
        for line in stream:
            r = json.loads(line)
            key = (r['station'], r['record'])
            if r['accepted']:
                prior = previous_rows.get(key)
                assert prior and prior['week'] == r['week'] - 1
                assert prior['localScore'] >= 100 and prior['localUnits'] >= 20 and prior['localPanel'] > 0
            previous_rows[key] = r
            if r['sourceWeek'] is not None:
                assert r['sourceWeek'] == r['week'] - 1
                sender = r.get('sourceStation')
                if sender:
                    assert sender != r['station'] and key not in heard
                    heard.add(key)
                    sender_calls[(r['week'], sender)] += 1
                else:
                    assert r['sourceRegion'] != r['region']
                report_count += 1
                offers[(r['week'], r['record'])] += 1
            if r['seat'] == 'research-trial':
                active_seats[(r['week'], r['station'])] += 1
                if key not in starts:
                    assert r['offer'] and not r['history']
                    starts[key] = r['week']
                    if r.get('geography'):
                        assert r['tipMayAudition'] and r.get('sourceStation')
                        geographic_starts.add(key)
                        if r['geography'] == 'awaiting-arrival': distant_starts.add(key)
                    if not r['connected']: below.add(key)
                    if r['localPanel'] == 0: silent.add(key)
                else:
                    assert r['active'] and r['start'] == starts[key]
                assert 0 <= r['week'] - starts[key] < 3
            if r['seat'] == 'research-graduated':
                assert key in starts and r['accepted'] and r['active']
                assert r['week'] - starts[key] >= 2
                assert r['localScore'] >= 100 and r['localUnits'] >= 20 and r['localPanel'] > 0
                graduated.add(key)
            if r['history'] and r['seat'] in ('research-graduated', 'ranked', 'readd'):
                ordinary_weeks[key].add(r['week'])
            if r['active'] and r['seat'] is None:
                rejected.add(key)
                (capacity_rejected if r['accepted'] else response_rejected).add(key)
            accepted_count += bool(r['accepted'])
    assert max(active_seats.values(), default=0) <= 1
    if not sender_calls:
        assert max(offers.values(), default=0) <= 2
    assert max(sender_calls.values(), default=0) <= 2
    complete = {key for key, start in starts.items() if start <= run['weeks'] - 3}
    persistence_cohort = {key for key, start in starts.items() if start <= run['weeks'] - 4}
    sustained = {key for key, weeks in ordinary_weeks.items() if any(w+1 in weeks for w in weeks)}
    retired_trials = set()
    missing = complete - graduated - rejected
    if missing:
        retirement = ROOT / 'SimLogs' / (run['name'] + '-retirement.csv')
        if not retirement.exists(): retirement = pathlib.Path(str(retirement) + '.gz')
        events = pd.read_csv(retirement, usecols=['status', 'week', 'recordId'])
        final_week = events[events.status.eq('retired')].set_index('recordId').week
        # A withdrawn title can leave the active list immediately after the meeting.
        # Require the actual retirement on the last observed trial week.
        retired_trials = {key for key in missing if previous_rows[key]['seat'] == 'research-trial'
                          and final_week.get(key[1]) == previous_rows[key]['week']}
    assert not (missing - retired_trials), 'Mature trial has no terminal decision or documented retirement'
    out.update(reports=report_count, starts=len(starts), graduated=len(graduated),
               rejected=len(rejected), retiredDuringTrial=len(retired_trials),
               unresolved=len(set(starts)-graduated-rejected-retired_trials),
               belowThresholdStarts=len(below), belowThresholdGraduated=len(below & graduated),
               acceptedObservations=accepted_count, matureStarts=len(complete),
               matureGraduated=len(complete & graduated), matureRejected=len(complete & rejected),
               matureGraduationPct=100*len(complete & graduated)/len(complete) if complete else None,
               matureBelowStarts=len(complete & below), matureBelowGraduated=len(complete & below & graduated),
               persistenceStarts=len(persistence_cohort), sustainedOrdinary=len(persistence_cohort & sustained),
               sustainedOrdinaryPct=100*len(persistence_cohort & sustained)/len(persistence_cohort) if persistence_cohort else None,
               matureSilentStarts=len(complete & silent), matureSilentGraduated=len(complete & silent & graduated),
               matureAcceptedButUnseated=len(complete & capacity_rejected),
               matureWithoutAcceptance=len(complete & response_rejected))
    if sender_calls:
        out.update(namedTips=len(heard), geographicStarts=len(geographic_starts), heldRegionStarts=len(distant_starts),
                   geographicGraduations=len(geographic_starts & graduated), heldRegionGraduations=len(distant_starts & graduated))
    return out

def main():
    p = argparse.ArgumentParser(); p.add_argument('tag'); a = p.parse_args()
    folder = ROOT / 'SimLogs' / a.tag
    plan = json.loads((folder/'plan.json').read_text())
    rows = []
    for run in plan:
        receipt = json.loads((folder/(run['name']+'-receipt.json')).read_text())
        assert receipt['complete'] and receipt['exitCode'] == 0 and receipt['assembly'] == receipt['assemblyAfter']
        rows.append(analyze(run))
    frame = pd.DataFrame(rows)
    frame.to_csv(folder/'audition-metrics.csv', index=False)
    numeric = frame.select_dtypes('number').columns.drop(['seed', 'weeks'])
    frame.groupby('arm')[numeric].mean().to_csv(folder/'audition-means.csv')
    random = frame[frame.arm.eq('audition_random')].set_index('seed')
    response = frame[frame.arm.eq('audition_response')].set_index('seed')
    delta = response[numeric].subtract(random[numeric])
    pd.DataFrame({'delta': delta.mean(), 'se': delta.std()/len(delta)**.5,
                  'n': delta.count()}).to_csv(folder/'audition-paired.csv')
    (folder/'audition-validation.json').write_text(json.dumps({'completedRuns': len(rows),
        'checked': ['report lag', 'cross-region source', 'report limit', 'trial capacity',
                    'duration', 'no restart', 'two local response observations', 'mature terminal outcomes'],
        'violations': 0}, indent=2))
    print(frame.to_string(index=False))

if __name__ == '__main__': main()
