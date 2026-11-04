"""Validate research telemetry and completed-world accounting, including zero-source cases."""
import argparse, csv, json, pathlib
import numpy as np
import pandas as pd

ROOT=pathlib.Path(__file__).resolve().parents[1]

def main():
    p=argparse.ArgumentParser()
    p.add_argument('tag')
    p.add_argument('--identity',action='store_true')
    a=p.parse_args(); folder=ROOT/'SimLogs'/a.tag
    receipts=list(folder.glob('*-receipt.json')); results=[]
    for path in receipts:
        r=json.loads(path.read_text())
        assert r['complete'] and r['exitCode']==0 and r['assembly']==r['assemblyAfter'],path
        pre=ROOT/'SimLogs'/r['name']
        args=r['args']
        def dose(name):
            return next((float(x.split('=',1)[1]) for x in args if x.startswith('--local-consumption-'+name+'=')),0)
        c,b,f=dose('contact'),dose('peer'),dose('familiarity')
        trace=pathlib.Path(str(pre)+'-local-consumption.csv')
        n=0
        if trace.exists():
            x=pd.read_csv(trace); n=len(x)
            assert np.isfinite(x.select_dtypes('number')).all().all()
            assert x[['panel','aware','afterAware']].ge(-1e-6).all().all()
            assert x[['panel','aware','afterAware']].le(1.000001).all().all()
            assert (x.rawAfter>=-1e-6).all()
            cap=x.audience*x.conversion*(1+f)
            assert (x.rawAfter<=cap+np.maximum(1e-3,cap*2e-6)).all()
            if c==b==f==0:
                assert x.rawBefore.equals(x.rawAfter),'Observe/other-path arm changed raw demand in observer'
            else:
                zero=((x.panel.eq(0)| (c==0)) & (x.priorBuyers.eq(0)|(b==0)) & (x.listening.eq(0)|(f==0)))
                assert np.allclose(x.loc[zero,'rawBefore'],x.loc[zero,'rawAfter'],rtol=2e-6,atol=1e-3)
        market=pd.read_csv(str(pre)+'-market-clearing-weekly.csv')
        errors=market[['inventoryViolationCount','allocationViolationCount','reconciliationDelta','settlementDelta']].abs().sum().sum()
        assert errors==0,(r['name'],errors)
        assert (market.clearedTotalUnits==market.clearedSingleUnits+market.clearedAlbumUnits).all()
        results.append(dict(name=r['name'],probeRows=n,integrityErrors=float(errors)))
    identity={}
    if a.identity:
        old=ROOT/'SimLogs/hs52op1001-SMP-HGRPO-djb24u-pp'
        new=ROOT/'SimLogs/lrc12-v2-pp-1001'
        for suffix in ['records','station-playlists','label-finance']:
            with open(str(old)+'-'+suffix+'.csv',newline='') as stream:
                before=[row for row in csv.DictReader(stream) if int(row['week'])<=12]
            with open(str(new)+'-'+suffix+'.csv',newline='') as stream:
                after=list(csv.DictReader(stream))
            assert before==after,suffix
            identity[suffix]=dict(equal=True,rows=len(before))
    output=dict(completedRuns=len(results),results=results,offIdentity=identity)
    (folder/'validation.json').write_text(json.dumps(output,indent=2))
    print(json.dumps(dict(completedRuns=len(results),probeRows=sum(r['probeRows'] for r in results),integrityErrors=0,offIdentity=identity),indent=2))

if __name__=='__main__':main()
