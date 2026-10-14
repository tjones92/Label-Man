import csv,json
from pathlib import Path
from collections import defaultdict,Counter
root=Path(__file__).resolve().parents[1]/'SimLogs'
def rows(name,suffix):
    with (root/f'{name}-{suffix}.csv').open(encoding='utf-8-sig',newline='') as f: return list(csv.DictReader(f))
out=[]
for seed in (1001,2002):
    modes={}
    for mode in ('off','recruitment'):
        name=f'scene4-four-year-v1-{mode}-{seed}'
        hq={r['labelId']:r['hqPlaceId'] for phase in ('start','end') for r in rows(name,'scene-recruitment-labels-'+phase)}
        names={r['labelId']:r['labelName'] for r in rows(name,'label-finance')}
        groups=defaultdict(Counter); labels=Counter(); failures=Counter(); total=0; active=0
        for r in rows(name,'label-scouting-vacancy-weekly'):
            if r['labelId'] in {'label_0026','label_0028'} or r['isActiveLabel']!='true': continue
            active+=1
            if r['canAffordEstimatedAdvance']!='true': continue
            slots=int(r['unusedOperatingRosterSlots']); total+=slots
            if not slots: continue
            label=r['labelId']; labels[label]+=slots
            for group,key in [('hq',hq.get(label) or 'Unknown'),('tier',r['labelTier']),('year',r['year']),('origin',r['labelOrigin'])]: groups[group][key]+=slots
            failures[r['failureReason'] or 'None']+=slots
        modes[mode]=dict(snapshotSlots=total,activeLabelWeeks=active,slotsPerActiveLabelWeek=total/active,groups={k:dict(v) for k,v in groups.items()},failures=dict(failures),labels=labels,names=names,hq=hq)
    a,b=modes['off'],modes['recruitment']
    changes=sorted(((k,b['labels'][k]-a['labels'][k]) for k in set(a['labels'])|set(b['labels'])),key=lambda x:-x[1])
    deltas={g:{k:b['groups'][g].get(k,0)-a['groups'][g].get(k,0) for k in set(a['groups'][g])|set(b['groups'][g])} for g in a['groups']}
    out.append(dict(seed=seed,modes={m:{k:v for k,v in d.items() if k not in ('labels','names','hq')} for m,d in modes.items()},deltas=deltas,topLabels=[dict(id=k,name=b['names'].get(k,a['names'].get(k)),hq=b['hq'].get(k),excess=v) for k,v in changes[:15]]))
(root/'scene-vacancy-diagnosis.json').write_text(json.dumps(out,indent=2)+'\n')
print('Wrote',root/'scene-vacancy-diagnosis.json')
