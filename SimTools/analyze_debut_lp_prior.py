"""Matched debut-LP prior treatment in the broad combined configuration.

Control and treatment share the assembly and every explicit flag except
--album-prior-debut-scale=. The flag-off control is also compared with the
original combined worlds to show the new build is inert with the flag off.
"""
import argparse
import hashlib
import json
import statistics as st
from collections import Counter, defaultdict

import analyze_writing_full_access as access
from analyze_lp_assembly_turnover import lp_outputs
from analyze_writing_combined_integration import economy
from analyze_writing_concentration import iter_rows

SWITCH = '--album-prior-debut-scale='
SMALLER = ('Small', 'Independent', 'Boutique')
INERT_SUFFIXES = ('label-finance', 'release-slate', 'release-profit-gates', 'records', 'company-monthly')


def manifest(run):
    return json.loads((access.CAL / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))


def validate_pair(control, treatment):
    def flags(m):
        fs = m['flags']
        if len(fs) != len({f.split('=')[0] for f in fs}):
            raise ValueError('Duplicate explicit flag keys')
        return {f for f in fs if not f.startswith('--run=')}
    cf, tf = flags(control), flags(treatment)
    if control['assemblySha256'] != treatment['assemblySha256']:
        raise ValueError('Mixed assemblies')
    added = tf - cf
    if cf - tf or len(added) != 1 or not next(iter(added)).startswith(SWITCH) or '--album-prior-recalibration' not in cf:
        raise ValueError('Expected the debut LP prior scale as the only added flag')
    return float(next(iter(added))[len(SWITCH):])


def check_complete(run):
    log = (access.CAL / f'{run}.log').read_text(encoding='utf-8-sig')
    errors = (access.CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
    if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
        raise ValueError(f'{run}: incomplete or engine errors')


def digest(run, suffix):
    path = access.CAL.parent / f'{run}-{suffix}.csv'
    h = hashlib.sha256()
    with path.open('rb') as f:
        for line in f:
            h.update(line.replace(f'{run}'.encode(), b'RUN'))
    return h.hexdigest()


def inertness(original, off):
    return {s: digest(original, s) == digest(off, s) for s in INERT_SUFFIXES}


def lp_funnel(run):
    tiers = {r['labelId']: r['initialTier'] for r in iter_rows(run, 'label-directory')}
    gates, slots = Counter(), defaultdict(set)
    for r in iter_rows(run, 'release-profit-gates'):
        if r['strategy'] == 'AlbumStandalone' and tiers.get(r['labelId']) in SMALLER:
            gates[(r['careerState'], 'n')] += 1
            gates[(r['careerState'], 'commit')] += r['commit'] == 'true'
    for r in iter_rows(run, 'release-slate'):
        if r['format'] == 'Album' and tiers.get(r['labelId']) in SMALLER:
            slots[r['state']].add(r['slotId'])
    return dict(gatePassByCareer={c: [gates[(c, 'commit')], gates[(c, 'n')]] for c in sorted({k[0] for k in gates})},
                smallerLpSlots={s: len(slots[s]) for s in ('Proposed', 'InPreparation', 'Ready', 'Issued', 'Cancelled')})


def early_closures(run, weeks=13):
    tiers = {r['labelId']: r['initialTier'] for r in iter_rows(run, 'label-directory')}
    units = defaultdict(float)
    for r in iter_rows(run, 'label-sales-weekly'):
        if int(r['week']) <= weeks:
            units[r['labelId']] += float(r['units'])
    first, last = {}, {}
    for r in iter_rows(run, 'label-finance'):
        first.setdefault(r['labelId'], r)
        last[r['labelId']] = r
    out = Counter()
    for lid, r in last.items():
        if first[lid]['week'] != '1' or tiers.get(lid) not in SMALLER:
            continue
        u = units.get(lid, 0)
        b = 'none' if u < 1 else '1-10k' if u < 10e3 else '10-30k' if u < 30e3 else '30k+'
        out[(b, 'n')] += 1
        out[(b, 'closed')] += r['status'] == 'Defunct'
    return {b: [out[(b, 'closed')], out[(b, 'n')]] for b in ('none', '1-10k', '10-30k', '30k+')}


def lp_realized(run, max_release_week=52):
    tiers = {r['labelId']: r['initialTier'] for r in iter_rows(run, 'label-directory')}
    assembly = {r['recordId']: r for r in iter_rows(run, 'lp-assembly') if r['recordId']}
    outcomes = {r['recordId']: r for r in iter_rows(run, 'release-all-in-outcomes') if r['format'] == 'Album'}
    groups = defaultdict(list)
    for r in iter_rows(run, 'release-campaign-costs'):
        o, a = outcomes.get(r['recordId']), assembly.get(r['recordId'])
        if not o or not a or int(r['week']) > max_release_week or tiers.get(r['labelId']) not in SMALLER:
            continue
        groups[a['careerState']].append((float(r['releaseTimeExpectedNet']), float(o['realizedNet'])))
    return {c: dict(n=len(v), medianForecastNet=st.median(x[0] for x in v), medianRealizedNet=st.median(x[1] for x in v),
                    profitable=sum(x[1] > 0 for x in v), totalRealizedNet=sum(x[1] for x in v))
            for c, v in groups.items()}


def professional_lp_use(run):
    opening, first = set(), {}
    for r in iter_rows(run, 'writing-demos'):
        if r['status'] != 'Recorded':
            continue
        if int(r['week']) == 0:
            opening.add(r['songId'])
        elif r['songId'] not in opening and r.get('source', 'ExternalProfessional') == 'ExternalProfessional':
            first.setdefault((r['songId'], r['artistId']), r)
    singles = {(r['songId'], r['artistId']) for r in iter_rows(run, 'writing-launches')
               if int(r['week']) > 0 and r['source'] == 'ExternalProfessional'}
    albums = {(r['songId'], r['artistId']) for r in iter_rows(run, 'writing-album-tracks')
              if r['terminalState'] == 'Released' and r['source'] == 'ExternalProfessional'}
    keys = set(first)
    return dict(newProfessionalDemos=len(keys), onSingle=len(keys & singles), albumOnly=len((keys & albums) - singles))


def arm(run):
    a = access.arm(run)
    return dict(run=run, assemblySha256=a['workflow']['assemblySha256'], matureIntentions=a['matureIntentions'],
                economy=economy(run), lpOutputs=lp_outputs(run), lpFunnel=lp_funnel(run),
                earlyClosures=early_closures(run), lpRealized=lp_realized(run), professionalLpUse=professional_lp_use(run))


def compare(control_tag, treatment_tag, original_tag, seeds=(1001, 2002)):
    pairs = []
    for seed in seeds:
        control, treatment, original = (f'{t}-market-{seed}' for t in (control_tag, treatment_tag, original_tag))
        for run in (control, treatment):
            check_complete(run)
        scale = validate_pair(manifest(control), manifest(treatment))
        pairs.append(dict(seed=seed, scale=scale, flagOffMatchesOriginal=inertness(original, control),
                          control=arm(control), treatment=arm(treatment)))
    pooled = {}
    for side in ('control', 'treatment'):
        counts = Counter()
        for p in pairs:
            counts.update(p[side]['matureIntentions']['sources'])
        pooled[side] = access.mix(counts)
    return dict(controlTag=control_tag, treatmentTag=treatment_tag, pairs=pairs, pooledMatureIntentions=pooled,
                contrast='Debut LP prior scale added; all other explicit flags and assembly hash match.')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--control-tag', default='writing-combined-debut-off')
    p.add_argument('--treatment-tag', default='writing-combined-debut-lp-prior')
    p.add_argument('--original-tag', default='writing-combined-integration')
    args = p.parse_args()
    result = compare(args.control_tag, args.treatment_tag, args.original_tag)
    (access.CAL / 'writing-debut-lp-prior-comparison.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, indent=1)[:200])
