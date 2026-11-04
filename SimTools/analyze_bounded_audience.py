"""Verify finite-audience accounting and report conditional fit diagnostics for live worlds."""
import argparse, json, pathlib
import numpy as np
import pandas as pd

ROOT = pathlib.Path(__file__).resolve().parents[1]

def analyze(run):
    path = ROOT/'SimLogs'/(run['name']+'-bounded-audience.csv')
    result = {k: run[k] for k in ('name','arm','seed','weeks')}
    if not path.exists(): return result
    x = pd.read_csv(path)
    assert len(x) and np.isfinite(x.select_dtypes('number')).all().all()
    assert not x.duplicated(['week','recordId','region']).any()
    assert x[['capacity','beforeInterest','beforeOwners','beforeEnthusiasts','acquired','newEnthusiasts',
              'anonymousIntent','fanIntent','cleared','fanCleared','anonymousCleared','owners','interested','enthusiasts']].ge(-1e-5).all().all()
    assert (x.beforeInterest+x.beforeOwners <= x.capacity+1e-5).all()
    assert (x.acquired <= x.capacity-x.beforeInterest-x.beforeOwners+1e-5).all()
    assert (x.anonymousIntent <= x.beforeInterest+x.acquired+1e-5).all()
    assert (x.anonymousCleared <= x.anonymousIntent+1e-5).all()
    assert (x.cleared <= np.floor(x.totalIntent)+1e-5).all()
    assert (x.owners+x.interested <= x.capacity+1e-5).all()
    assert (x.enthusiasts <= x.capacity+1e-5).all()
    assert (x.fanOwners <= x.fanCapacity+1e-5).all()
    near = lambda a,b: np.allclose(a,b,rtol=1e-9,atol=1e-5)
    assert near(x.cleared,x.anonymousCleared+x.fanCleared)
    assert near(x.owners,x.beforeOwners+x.anonymousCleared)
    assert near(x.interested,(x.beforeInterest+x.acquired-x.anonymousCleared)*.9)
    assert near(x.enthusiasts,x.beforeEnthusiasts*.75+x.newEnthusiasts)
    assert near(x.totalIntent,x.anonymousIntent+x.fanIntent)
    g = x.groupby(['recordId','region'],sort=False)
    assert near(x.owners+x.fanOwners,g.cleared.cumsum())
    assert near(x.beforeOwners,g.owners.shift().fillna(0))
    assert near(x.beforeEnthusiasts,g.enthusiasts.shift().fillna(0))
    assert (g.capacity.nunique()==1).all()
    budgets=x.groupby(['week','region'])[['radioContacts','peerContacts']].sum()
    assert budgets.le(2.000001).all().all()
    if '--bounded-audience=radio' in run['args']: assert x.peerContacts.eq(0).all()
    # A same-state arithmetic diagnostic, not a fit-removal simulation or causal forecast.
    x['intentWithoutFit']=(x.beforeInterest+x.acquired)*(1-np.exp(-x.rate/x.fit.clip(lower=1e-12)))
    result.update(rows=len(x),clearedM=x.cleared.sum()/1e6,anonymousClearedM=x.anonymousCleared.sum()/1e6,
                  fanClearedM=x.fanCleared.sum()/1e6,anonymousIntentM=x.anonymousIntent.sum()/1e6,
                  legacyRawM=x.legacyRaw.sum()/1e6,peerContacts=x.peerContacts.sum(),
                  maxRadioBudget=budgets.radioContacts.max(),maxPeerBudget=budgets.peerContacts.max())
    for name,mask in [('connected',x.connected),('below',~x.connected)]:
        y=x[mask]
        result.update({name+'Rows':len(y),name+'RateMedian':y.rate.median(),name+'FitMedian':y.fit.median(),
                       name+'IntentM':y.anonymousIntent.sum()/1e6,name+'ClearedM':y.anonymousCleared.sum()/1e6,
                       name+'SameStateIntentWithoutFitM':y.intentWithoutFit.sum()/1e6})
    return result

def main():
    p=argparse.ArgumentParser(); p.add_argument('tag'); a=p.parse_args()
    folder=ROOT/'SimLogs'/a.tag
    plan=json.loads((folder/'plan.json').read_text()); rows=[]
    for r in plan:
        receipt=json.loads((folder/(r['name']+'-receipt.json')).read_text())
        assert receipt['complete'] and receipt['exitCode']==0 and receipt['assembly']==receipt['assemblyAfter']
        rows.append(analyze(r))
    frame=pd.DataFrame(rows); frame.to_csv(folder/'audience-metrics.csv',index=False)
    frame.groupby('arm')[frame.select_dtypes('number').columns.drop('seed')].mean().to_csv(folder/'audience-means.csv')
    (folder/'audience-validation.json').write_text(json.dumps(dict(completedRuns=len(rows),
        audienceRows=int(frame.rows.sum()),violations=0,checks=['finite population','owner ledger',
        'fan partition','integer serviceable cap','settlement only','interest and enthusiasm transitions',
        'fixed capacity','shared radio and peer contact budgets']),indent=2))
    print(frame.round(4).to_string(index=False))

if __name__=='__main__': main()
