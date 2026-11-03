"""Fixed-chart sensitivity replay, not a simulation or estimated policy effect."""
import csv, json, sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def read(run, suffix):
    with (ROOT/'SimLogs'/f'{run}-{suffix}.csv').open(encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f))

def replay(run):
    chart = [r for r in read(run, 'records') if 1 <= int(r['currentPosition']) <= 100]
    end = max(int(r['week']) for r in chart)
    assert end == 104
    weeks = defaultdict(list)
    for r in chart: weeks[int(r['week'])].append(r)
    assert all({int(r['currentPosition']) for r in weeks[w]} == set(range(1, 101)) and len(weeks[w]) == 100 for w in range(1,end+1))
    opening = {r['recordId'] for r in weeks[0]}
    launches = {r['recordId']:r for r in read(run, 'writing-launches')}
    first = {}
    for r in sorted(chart, key=lambda x:int(x['week'])):
        if int(r['currentPosition']) <= 40: first.setdefault(r['recordId'], r)
    first_actor = {}
    for rid,r in first.items():
        if rid not in opening and int(r['week']) > 0:
            first_actor.setdefault(r['artistId'], int(r['week']))
    mature = {a:w for a,w in first_actor.items() if w+52 <= end}
    # Eligibility uses the baseline world's first observed non-opening hit and
    # baseline lifetime hit count at launch; no future information is used.
    classes = {}
    for rid,r in launches.items():
        w=int(r['week']); prior=int(r['priorTop40']); hit=first_actor.get(r['artistId'])
        classes[rid] = ('middle' if hit is not None and hit < w <= hit+52 and 1 <= prior <= 3
                        else 'established' if prior >= 4 else 'other')
    scenarios = {'baseline':(1.,1.), 'middle_50pct':(1.5,1.),
                 'middle_100pct':(2.,1.), 'established_half':(1.,.5),
                 'combined':(2.,.5)}
    results={}
    mismatches=0
    for w in range(1,end+1):
        ranked=sorted(weeks[w], key=lambda r:-float(r['chartPoints']))
        mismatches+=sum(int(r['currentPosition']) != i for i,r in enumerate(ranked,1))
    for name,(mid,star) in scenarios.items():
        first_counter={}; slots=Counter(); changed=0; eligible_slots=0
        for w in range(1,end+1):
            rows=weeks[w]
            # Preserve actual ordering at factor 1: assign the observed score
            # distribution in rank order because logged points do not reproduce rank.
            points=sorted((float(r['chartPoints']) for r in rows), reverse=True)
            proxy={int(r['currentPosition']):points[i] for i,r in enumerate(sorted(rows,key=lambda r:int(r['currentPosition'])))}
            def score(r):
                group=classes.get(r['recordId'],'other')
                return proxy[int(r['currentPosition'])]*(mid if group=='middle' else star if group=='established' else 1.)
            # Original position resolves equal adjusted points consistently.
            ranked=sorted(rows,key=lambda r:(-score(r),int(r['currentPosition'])))
            top=ranked[:40]
            changed+=len({r['recordId'] for r in top} - {r['recordId'] for r in rows if int(r['currentPosition'])<=40})
            eligible_slots+=sum(classes.get(r['recordId'])=='middle' for r in top)
            for r in top:
                slots[r['artistId']]+=1
                if r['recordId'] not in opening: first_counter.setdefault(r['recordId'],r)
        subsequent=Counter()
        for r in first_counter.values():
            a=r['artistId']; w=int(r['week']); origin=mature.get(a)
            if origin is not None and origin < w <= origin+52: subsequent[a]+=1
        if name == 'baseline': assert changed == 0
        results[name]={'cohort':len(mature),'one_to_three':sum(1<=subsequent[a]<=3 for a in mature),
                       'zero':sum(subsequent[a]==0 for a in mature),
                       'four_plus':sum(subsequent[a]>=4 for a in mature),
                       'top40_slots_for_eligible_followups':eligible_slots,
                       'top40_slot_replacements':changed,
                       'fixed_top30_top40_slot_share':sum(sorted(slots.values(),reverse=True)[:30])/(40*end),
                       'distinct_nonopening_top40_titles':len(first_counter)}
    return {'run':run,'rank_mismatches':mismatches,'eligible_launched_titles':sum(v=='middle' for v in classes.values()),'scenarios':results}

if __name__ == '__main__':
    output=[replay(run) for run in sys.argv[1:]]
    print(json.dumps(output,indent=2))
