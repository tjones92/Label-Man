"""Matched 104-week development gate, preregistered before treatment results."""
import csv, json, re, sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TAG = sys.argv[1] if len(sys.argv) > 1 else 'scene5-two-year-pressure-v1'
OUT = ROOT / 'SimLogs' / TAG

def rows(name, suffix):
    with (ROOT / 'SimLogs' / f'{name}-{suffix}.csv').open(encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f))

def summarize(name):
    population = [r for r in rows(name, 'artist-population-weekly') if r['labelTier'] == 'All']
    labor = rows(name, 'artist-labor-market-weekly')
    finance = rows(name, 'label-finance')
    revenue = [r for r in rows(name, 'market-revenue') if r['period'] == 'weekly']
    last = max(int(r['week']) for r in finance)
    final = [r for r in finance if int(r['week']) == last]
    integrity = {}
    for key in ('ownershipConflicts', 'duplicateRosterEntries', 'duplicatePoolEntries', 'terminalRostered', 'terminalReleaseEligible'):
        integrity[key] = max(int(r[key]) for r in population)
    for key in ('duplicateSeekingEntries', 'latentUnsignedPoolEntries', 'seekingMissingFromUnsignedPool', 'prospectStatusContractConflicts'):
        integrity[key] = max(int(r[key]) for r in labor)
    annual = defaultdict(lambda: defaultdict(float))
    tiers = defaultdict(lambda: defaultdict(float))
    for r in revenue:
        metric = {'Single':'singleUnits', 'Album':'albumUnits', 'All':'marketNet'}.get(r['releaseFormat'])
        if not metric: continue
        value = float(r['marketNet' if metric == 'marketNet' else 'totalMarketUnits'])
        if r['labelTier'] == 'All': annual[r['year']][metric] += value
        else: tiers[r['labelTier']][metric] += value
    totals = {m:sum(y[m] for y in annual.values()) for m in ('singleUnits','albumUnits','marketNet')}
    totals.update(endRostered=int(population[-1]['rostered']), endRegistry=int(population[-1]['registryTotal']),
        formations=sum(int(r['formedThisWeek']) for r in population), firstSignings=sum(int(r['firstTimeSignings']) for r in population),
        vacancyWeeks=sum(int(r['affordableHiringVacancies']) for r in labor),
        defunct=sum(r['status']=='Defunct' for r in final), labels=len(final))
    for tier in tiers:
        labels = [r for r in final if r['labelTier']==tier]
        tiers[tier].update(labels=len(labels), defunct=sum(r['status']=='Defunct' for r in labels),
            finalCash=sum(float(r['cashReserves']) for r in labels))
    evidence = rows(name, 'scene-recruitment-end')
    unexplained = sum(r['route'] not in {'Catchment','RoadCircuit','NationalAr','LocalVisitor'} for r in evidence)
    log = (OUT/f'{name}-console.log').read_text(encoding='utf-8-sig')
    room = re.findall(r'SCENE_ROOM_CENSUS phase=end .*duplicateBills=(\d+) personConflicts=(\d+) remoteDays=(\d+) capacityErrors=(\d+)', log)
    assert len(room)==1, 'Missing final room integrity census'
    integrity.update(zip(('duplicateBills','personConflicts','remoteDays','capacityErrors'),map(int,room[0])))
    errors = (OUT/f'{name}-errors.log').read_text(encoding='utf-8-sig')
    unexpected_errors = [line for line in errors.splitlines() if line.startswith('ERROR:')
        and "Failed to instantiate an autoload, script 'res://Systems/MissingSingletonsTemp.cs' does not inherit from 'Node'." not in line]
    integrity['unexpectedRuntimeErrors'] = len(unexpected_errors)
    first = min(int(r['week']) for r in finance)
    opening_majors = {r['labelId']:r['labelName'] for r in finance if int(r['week'])==first and r['labelTier']=='Major'}
    major_net = {label:0.0 for label in opening_majors}
    for r in finance:
        if r['labelId'] in major_net: major_net[r['labelId']] += float(r['weeklyNet'])
    opening_hq = {r['labelId']:r['hqPlaceId'] for r in rows(name,'scene-recruitment-labels-start')}
    cohort_major_net = [dict(labelId=label,name=opening_majors[label],hqPlaceId=opening_hq.get(label),financeWeeklyNetSum=value)
        for label,value in major_net.items()]
    availability = rows(name,'scene-recruitment-labels-end')
    vacancies = [r for r in availability if r['active']=='True' and int(r['unfilledSlots'])>0]
    # User clarified these two legacy UK majors are accepted exceptions to the US-only scope.
    accepted_legacy_uk = {'label_0026','label_0028'}
    vacancy_observations = rows(name,'label-scouting-vacancy-weekly')
    legacy_vacancy_proxy = sum(int(r['unusedOperatingRosterSlots']) for r in vacancy_observations
        if r['labelId'] in accepted_legacy_uk and r['isActiveLabel']=='true' and r['canAffordEstimatedAdvance']=='true')
    totals['acceptedLegacyUkVacancyWeeks'] = legacy_vacancy_proxy
    totals['vacancyWeeksExcludingAcceptedLegacyUk'] = totals['vacancyWeeks'] - legacy_vacancy_proxy
    return dict(totals=totals, annual=dict(annual), tiers=dict(tiers), integrity=integrity,
        unexplainedSignings=unexplained, routes=dict(Counter(r['route'] for r in evidence)), unexpectedErrors=unexpected_errors,
        openingMajorCohortFinanceNet=cohort_major_net, endActiveVacantLabels=vacancies,
        endVacantLabelsByHq=dict(Counter(r['hqPlaceId'] or 'Unknown' for r in vacancies)))

def pct(before, after):
    return 100*(after-before)/abs(before) if before else None

manifest = json.loads((OUT/'runs.json').read_text(encoding='utf-8-sig'))
assert len(manifest['results']) == 4 and all(r['passed'] and r['weeks']==104 for r in manifest['results'])
report = dict(weeks=104, seeds=[1001,2002], revision=manifest['revision'],
    thresholds=dict(economicDeclinePercent=5, finalRosterDeclinePercent=5, defunctIncreasePoints=5,
        vacancyExcess='greater than max(25% of control vacancy-weeks, 104 vacancy-weeks)'),
    statisticalSignificanceClaim=False, decadeValidationDeferred=True,
    userAcceptedLegacyExceptions=['EMI Records (label_0028)', 'Decca UK Records (label_0026)'], comparisons=[])
for seed in report['seeds']:
    control = summarize(f'{TAG}-off-{seed}')
    candidate = summarize(f'{TAG}-recruitment-{seed}')
    a,b = control['totals'], candidate['totals']
    deltas = {k:pct(a[k],b[k]) for k in ('singleUnits','albumUnits','marketNet','endRostered','formations','vacancyWeeks')}
    failure_points = 100*b['defunct']/b['labels'] - 100*a['defunct']/a['labels']
    failures = [k for k in ('singleUnits','albumUnits','marketNet','endRostered') if deltas[k] is not None and deltas[k] < -5]
    if failure_points > 5: failures.append('labelFailures')
    us_a,us_b = a['vacancyWeeksExcludingAcceptedLegacyUk'],b['vacancyWeeksExcludingAcceptedLegacyUk']
    if us_b-us_a > max(.25*us_a,104): failures.append('affordableVacanciesExcludingAcceptedLegacyUk')
    if any(candidate['integrity'].values()) or any(control['integrity'].values()): failures.append('integrity')
    if candidate['unexplainedSignings']: failures.append('unexplainedSignings')
    annual_deltas = {year:{k:pct(control['annual'][year][k],values[k]) for k in values}
        for year,values in candidate['annual'].items()}
    tier_deltas = {tier:{k:pct(control['tiers'][tier][k],values[k]) for k in ('singleUnits','albumUnits','marketNet')}
        for tier,values in candidate['tiers'].items()}
    major_a = sum(r['financeWeeklyNetSum'] for r in control['openingMajorCohortFinanceNet'] if r['labelId'] not in {'label_0026','label_0028'})
    major_b = sum(r['financeWeeklyNetSum'] for r in candidate['openingMajorCohortFinanceNet'] if r['labelId'] not in {'label_0026','label_0028'})
    report['comparisons'].append(dict(seed=seed,control=control,candidate=candidate,percentChanges=deltas,
        defunctIncreasePoints=failure_points, annualPercentChanges=annual_deltas,tierPercentChanges=tier_deltas,
        openingMajorsExcludingLegacyUkFinanceNet=dict(control=major_a,recruitment=major_b,percentChange=pct(major_a,major_b)),
        acceptedLegacyUkAdjustedVacancyChangePercent=pct(us_a,us_b), failedGates=failures))
report['developmentGatePassed'] = all(not r['failedGates'] for r in report['comparisons'])
report['economicGatePassed'] = all(not [g for g in r['failedGates'] if g != 'affordableVacanciesExcludingAcceptedLegacyUk']
    for r in report['comparisons'])
report['recruitmentPressureScreenPassed'] = all('affordableVacanciesExcludingAcceptedLegacyUk' not in r['failedGates']
    for r in report['comparisons'])
(OUT/'analysis.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
lines = ['# Phase 5 continuation: matched two-year pressure revalidation', '',
    f"Revision: `{manifest['revision']}`. Four complete 104-week runs, seeds 1001 and 2002.", '',
    'Controls and treatments use the current city placement build. Recruitment is the only flag changed within each pair.', '',
    'The practical screen was set before treatment results: declines over 5% in cumulative singles, albums, market net or final roster headcount; label failure-rate increase over 5 percentage points; affordable vacancy excess over max(25% of control, 104 label-weeks). Integrity and unexplained signing counts must be zero. Annual and tier results require review. Two seeds do not establish statistical significance.', '',
    '| Measure | Seed 1001 control → recruitment | Seed 2002 control → recruitment |',
    '|---|---:|---:|']
for metric in ('singleUnits','albumUnits','marketNet','endRostered','defunct','vacancyWeeks','vacancyWeeksExcludingAcceptedLegacyUk'):
    cells=[]
    for item in report['comparisons']:
        before=item['control']['totals'][metric]; after=item['candidate']['totals'][metric]
        change=pct(before,after)
        cells.append(f'{before:,.0f} → {after:,.0f}' + (f' ({change:+.2f}%)' if change is not None else ''))
    lines.append('| '+metric+' | '+' | '.join(cells)+' |')
lines += ['', f"Economic gate passed: **{report['economicGatePassed']}**. Recruitment pressure screen passed: **{report['recruitmentPressureScreenPassed']}**. Combined development screen passed: **{report['developmentGatePassed']}**.", '']
for item in report['comparisons']:
    lines.append(f"Seed {item['seed']} failed gates: {', '.join(item['failedGates']) or 'none'}. Integrity maxima: {item['candidate']['integrity']}. Unexplained signings: {item['candidate']['unexplainedSignings']}.")
    lines.append(f"Opening major cohort excluding accepted UK exceptions: finance-ledger net change {item['openingMajorsExcludingLegacyUkFinanceNet']['percentChange']:+.2f}%. This is a per-label finance metric, distinct from the market settlement net above.")
lines += ['', 'Scope clarification during testing: the user accepts EMI and Decca UK as legacy exceptions outside US-only recruitment. Their empty rosters and missing sales remain in raw totals and tier outputs. The vacancy screen subtracts their weekly active, affordable operating-target deficits from the aggregate demand count; the weekly vacancy telemetry is a snapshot proxy, not a new replay of the activation owner. Other unknown/foreign HQs remain visible. No economic threshold was changed.', '',
    'Full totals, annual and tier comparisons, route counts and exact manifests are in `analysis.json` and `runs.json` beside this report. Decade validation remains deferred. No gameplay constants or defaults were changed for this test.', '']
(OUT/'report.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='comparisons'},indent=2))
for r in report['comparisons']:
    print(json.dumps({k:r[k] for k in ('seed','percentChanges','defunctIncreasePoints','failedGates','annualPercentChanges','tierPercentChanges')},indent=2))


