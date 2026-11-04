"""Where do smaller labels lose LP projects in the combined profile?

Reads existing 104-week worlds only. Reports (1) the LP slate funnel for the
matched opening cohorts, (2) closure rates conditional on early unit sales,
(3) the share of LP preparation postponements that are all-in profit-gate
refusals, and (4) release-time LP forecast versus realized net by career.
"""
import csv
import json
import statistics as st
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LOGS = ROOT / 'SimLogs'
TURNOVER = LOGS / 'writing-calibration/writing-combined-label-turnover.json'
CONTROL = {1001: 'writing-full-profile-market-1001', 2002: 'writing-full-profile-extension-market-2002'}
COMBINED = {s: f'writing-combined-integration-market-{s}' for s in (1001, 2002)}
SMALLER = ('Small', 'Independent', 'Boutique')
STAGES = ('Proposed', 'InPreparation', 'Ready', 'Issued')


def rows(run, suffix):
    with open(LOGS / f'{run}-{suffix}.csv', newline='', encoding='utf-8') as f:
        yield from csv.DictReader(f)


def tiers(run):
    return {r['labelId']: r['initialTier'] for r in rows(run, 'label-directory')}


def cohort_funnel(identities, cohort):
    out = {'control': Counter(), 'combined': Counter()}
    for seed in (1001, 2002):
        for side, run, key in (('control', CONTROL[seed], 'controlId'), ('combined', COMBINED[seed], 'combinedId')):
            ends = {i[key]: i['anchorWeek'] for i in identities if i['seed'] == seed and i['cohort'] == cohort}
            reached = defaultdict(set)
            for r in rows(run, 'release-slate'):
                if r['format'] == 'Album' and r['labelId'] in ends and int(r['week']) <= ends[r['labelId']]:
                    reached[r['state']].add(r['slotId'])
                    if r['state'] == 'Postponed' and r['reason'] == 'insufficient-unreserved-cash':
                        out[side]['cashPostponementSlotWeeks'] += 1
            for s in STAGES:
                out[side][s] += len(reached[s])
    return out


def conditional_closure(identities):
    bins = ((1, 10e3), (10e3, 30e3), (30e3, float('inf')))
    out = {}
    for side, runs, key, exit_key in (('control', CONTROL, 'controlId', 'controlExitWeek'),
                                      ('combined', COMBINED, 'combinedId', 'combinedExitWeek')):
        units = {}
        for seed, run in runs.items():
            u = defaultdict(float)
            for r in rows(run, 'label-sales-weekly'):
                if int(r['week']) <= 13:
                    u[r['labelId']] += float(r['units'])
            units[seed] = u
        for lo, hi in bins:
            group = [i for i in identities if i['identity'][3] in SMALLER and lo <= units[i['seed']].get(i[key], 0) < hi]
            out[(side, lo)] = (sum(i[exit_key] is not None for i in group), len(group))
    return out


def gate_share_of_preparation_postponements():
    out = Counter()
    for run in COMBINED.values():
        t = tiers(run)
        refused = {(r['week'], r['labelId'], r['artistId']) for r in rows(run, 'release-profit-gates')
                   if r['strategy'] == 'AlbumStandalone' and r['commit'] == 'false'}
        for r in rows(run, 'release-slate'):
            if (r['format'] == 'Album' and t.get(r['labelId']) in SMALLER and r['state'] == 'Postponed'
                    and r['reason'] == 'preparation-material-economics-or-funding'):
                out['postponements'] += 1
                out['gateRefusals'] += (r['week'], r['labelId'], r['artistId']) in refused
    return out


def forecast_calibration(max_release_week=52):
    out = defaultdict(list)
    for run in COMBINED.values():
        t = tiers(run)
        assembly = {r['recordId']: r for r in rows(run, 'lp-assembly') if r['recordId']}
        outcomes = {r['recordId']: r for r in rows(run, 'release-all-in-outcomes') if r['format'] == 'Album'}
        for r in rows(run, 'release-campaign-costs'):
            o, a = outcomes.get(r['recordId']), assembly.get(r['recordId'])
            if not o or not a or int(r['week']) > max_release_week:
                continue
            cost = float(r['productionCost']) + float(r['marketingCost'])
            group = 'smaller' if t.get(r['labelId']) in SMALLER else 'other'
            out[(group, a['careerState'], a['kind'])].append(
                (float(r['releaseTimeExpectedNet']) + cost, float(o['realizedNet']) + cost, float(o['realizedNet'])))
    return out


def main():
    identities = json.loads(TURNOVER.read_text(encoding='utf-8'))['identities']
    for cohort in ('additionalExit', 'rescuedIdentity'):
        f = cohort_funnel(identities, cohort)
        print(f'LP slate funnel, {cohort} (through anchor week)')
        for s in STAGES + ('cashPostponementSlotWeeks',):
            print(f'  {s:26} control {f["control"][s]:6}  combined {f["combined"][s]:6}')
    print('Smaller-label closure by weeks 1-13 units')
    c = conditional_closure(identities)
    for lo, label in ((1, '1-10k'), (10e3, '10-30k'), (30e3, '30k+')):
        (a, n), (b, m) = c[('control', lo)], c[('combined', lo)]
        print(f'  {label:>7}: control {a}/{n} ({a / max(n, 1):.0%})  combined {b}/{m} ({b / max(m, 1):.0%})')
    g = gate_share_of_preparation_postponements()
    print(f'Smaller-label LP preparation postponements that are profit-gate refusals: {g["gateRefusals"]}/{g["postponements"]}')
    print('Album forecast vs realized, released by week 52 and finalized (contribution = net + production + marketing)')
    for k, v in sorted(forecast_calibration().items()):
        if len(v) < 5:
            continue
        print(f'  {"/".join(k):32} n={len(v):4} forecast {st.median(x[0] for x in v):7.0f} realized {st.median(x[1] for x in v):7.0f}'
              f'  ratio {st.median(x[1] / x[0] for x in v if x[0] > 0):.2f}  profitable {sum(x[2] > 0 for x in v) / len(v):.0%}')


if __name__ == '__main__':
    main()
