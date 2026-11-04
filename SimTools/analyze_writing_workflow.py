"""Join economic, material-use and complete career-cohort results for a frozen workflow matrix."""
import argparse
import json
from pathlib import Path

from analyze_writing_market import summarize as economy
from analyze_writing_concentration import summarize as concentration

LOGS = Path(__file__).resolve().parents[1] / 'SimLogs'


def rate(n, d):
    return n / d if d else None


def summarize(run, expected_weeks):
    manifest = json.loads((LOGS / 'writing-calibration' / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))
    profile = json.loads((LOGS / f'{run}-writing-profile.json').read_text(encoding='utf-8-sig'))
    e, c = economy(run), concentration(run, fixed_n=30)
    if e['weeks'] != expected_weeks or c['weeks'] != expected_weeks:
        raise ValueError(f'{run}: incomplete comparison window')
    if c['missingMaterialProvenance']:
        raise ValueError(f'{run}: missing release material provenance')
    if abs(e['top10PctActSlotShare'] - c['slotShare']) > 1e-12:
        raise ValueError(f'{run}: inconsistent concentration observations')
    material = c['material']
    if profile['PlannedAssignments'] and material['waitingPlansPreparedEarly']:
        raise ValueError(f'{run}: a linked plan was prepared before writing delivery')
    m = next((g for g in material['commissions'] if g['definition'] == 'all'), {})
    n = m.get('mature26WeekDeliveries', 0)
    sources = [g for g in c['conversions'] if g['definition'] == 'source']
    professional = next((g['releases'] for g in sources if g['group'] == 'ExternalProfessional'), 0)
    workflow = {(False,False):'control', (True,False):'planned', (False,True):'delivery', (True,True):'combined'}[
        profile['PlannedAssignments'],profile['DeliveryOffers']]
    return dict(run=run, seed=int(run.rsplit('-',1)[1]), workflow=workflow, assemblySha256=manifest['assemblySha256'],
        weeks=e['weeks'], orders=e['orders'], delivered=e['delivered'], pending=e['pending'],
        planLinked=e['linkedPlanOrders'], offered=e['offeredDeliveries'],
        waitingPlansPreparedEarly=material['waitingPlansPreparedEarly'],
        commissionFees=e['commissionFees'], licenseFees=e['licenseFees'], retainerFees=e['retainerFees'],
        totalWritingFees=e['commissionFees']+e['licenseFees']+e['retainerFees'], utilisation=e['meanPendingSlotShare'],
        unusedLicensed=e['unusedLicensedAtEnd'], matureDeliveries=n,
        shortlistExposure26=rate(m.get('shortlistedWithin26',0),n),
        retainedShortlistObserved26=rate(m.get('retainedWithin26',0),n),
        originalClientReleased26=rate(m.get('releasedWithin26',0),n),
        originalClientSingle26=rate(m.get('singleReleasedWithin26',0),n),
        linkedPlanIssued26=rate(m.get('linkedPlanIssued26',0),m.get('planLinked',0)),
        linkedPlanUsed26=rate(m.get('linkedPlanUsed26',0),m.get('planLinked',0)),
        originalActReleasedAtEnd=rate(m.get('releasedOriginalAct',0),n),
        otherActReleasedAtEnd=rate(m.get('releasedOtherAct',0),n),
        completeHitCohort=e['mature52WeekHitActs'], middleActs=e['oneToThreeFurtherHits52'],
        middleRate=rate(e['oneToThreeFurtherHits52'],e['mature52WeekHitActs']),
        silentAfterHit52=rate(e['noSingleReleaseAfterHit52'],e['mature52WeekHitActs']),
        top40Entries=e['top40Entries'], hitActs=e['hitActs'], leaderSlotShare=c['slotShare'], fixedTop30Share=c['fixedLeaderSlotShare'],
        finalOwnerC4=float(e['ownerConcentration'][-1]['c4ChartShare']) if e['ownerConcentration'] else None,
        failedLabels=e['labelsFailed'], observedLabels=e['labelsObserved'],
        professionalSingleShare=rate(professional,sum(g['releases'] for g in sources)),
        completedHolds=material['completedHolds'], destinations=material['deliveredDestinations'])


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('runs',nargs='+')
    p.add_argument('--weeks',type=int,default=104)
    p.add_argument('--output',type=Path,required=True)
    args = p.parse_args()
    result = [summarize(run,args.weeks) for run in args.runs]
    if len({r['assemblySha256'] for r in result}) != 1:
        raise ValueError('Workflow comparison must use one frozen assembly')
    keys = [(r['seed'],r['workflow']) for r in result]
    if len(set(keys)) != len(keys):
        raise ValueError('Duplicate seed/workflow in matrix')
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
