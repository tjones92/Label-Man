"""Professional-song balance screens against a flag-off combined control.

Each treatment tag must differ from the control only by flags named in
TREATMENT_FLAGS and share its assembly. Reports the issued single source mix
(mature intentions), covers per professional song overall and for the largest
genres, professional arrival outcomes, demo destinations, LP cut sources,
release volume, closures and professional single chart reach.
"""
import argparse
import json
from collections import Counter, defaultdict

import analyze_writing_full_access as access
from analyze_writing_combined_integration import economy
from analyze_writing_concentration import iter_rows

TREATMENT_FLAGS = ('--lp-filler-spares-fresh-demos', '--professional-family-pool', '--writing-market-capacity=', '--album-prior-debut-scale=',
                   '--writing-market-regional-offices', '--writing-market-extra-writers=',
                   '--writing-market-royalty-licences', '--writing-market-canonical-office-genres',
                   '--writing-market-specialist-offices', '--source-mix-standards-to-recent-hits',
                   '--source-mix-1960-pro-trims', '--tradpop-singles-new-songs',
                   '--programme-search-follows-mix', '--lp-assembly-gate-uncredited')
GENRES = ('RockAndRoll', 'TeenPop', 'RnB', 'Country', 'TraditionalPop', 'Soul', 'DooWop', 'EasyListening')


def manifest(run):
    return json.loads((access.CAL / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))


def validate(control, treatment):
    def flags(m):
        fs = m['flags']
        if len(fs) != len({f.split('=')[0] for f in fs}):
            raise ValueError('Duplicate explicit flag keys')
        return {f for f in fs if not f.startswith('--run=')}
    cf, tf = flags(control), flags(treatment)
    if control['assemblySha256'] != treatment['assemblySha256']:
        raise ValueError('Mixed assemblies')
    if cf - tf or any(not f.startswith(TREATMENT_FLAGS) for f in tf - cf):
        raise ValueError(f'Unexpected flag difference: {sorted(tf ^ cf)}')
    return sorted(tf - cf)


def check_complete(run):
    log = (access.CAL / f'{run}.log').read_text(encoding='utf-8-sig')
    errors = (access.CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
    if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
        raise ValueError(f'{run}: incomplete or engine errors')


def arrivals(run):
    out = Counter()
    for r in iter_rows(run, 'professional-arrivals'):
        o = r['outcome']
        out['licensed' if o == 'licensed' else 'commissioned' if o == 'commission-ordered' else 'empty']+= 1
    return dict(out)


def demo_destinations(run):
    opening, first = set(), set()
    for r in iter_rows(run, 'writing-demos'):
        if r['status'] != 'Recorded':
            continue
        (opening if int(r['week']) == 0 else first).add(r['songId'])
    first -= opening
    singles = {r['songId'] for r in iter_rows(run, 'writing-launches')
               if int(r['week']) > 0 and r['source'] == 'ExternalProfessional'}
    albums = {r['songId'] for r in iter_rows(run, 'writing-album-tracks')
              if r['terminalState'] == 'Released' and r['source'] == 'ExternalProfessional'}
    return dict(newRecorded=len(first), onSingle=len(first & singles), albumOnly=len((first & albums) - singles))


def album_cut_sources(run):
    return dict(Counter(r['source'] for r in iter_rows(run, 'writing-album-tracks') if r['terminalState'] == 'Released'))


def professional_chart(run):
    pro = {r['recordId'] for r in iter_rows(run, 'writing-launches') if int(r['week']) > 0 and r['source'] == 'ExternalProfessional'}
    launched = {r['recordId']: r['source'] for r in iter_rows(run, 'writing-launches') if int(r['week']) > 0}
    best = {}
    for r in iter_rows(run, 'records'):
        p = int(r['currentPosition'] or 0)
        if r['recordId'] in launched and p > 0:
            best[r['recordId']] = min(best.get(r['recordId'], 999), p)
    out = defaultdict(Counter)
    for rid, src in launched.items():
        group = 'professional' if rid in pro else 'cover' if src.startswith('Cover') else 'other'
        out[group]['launches'] += 1
        out[group]['top40'] += best.get(rid, 999) <= 40
        out[group]['top10'] += best.get(rid, 999) <= 10
    return {k: dict(v) for k, v in out.items()}


def ratio(sources):
    covers = sources.get('CoverStandard', 0) + sources.get('CoverRecentHit', 0)
    pro = sources.get('ExternalProfessional', 0)
    return round(covers / pro, 2) if pro else None


def arm(run):
    a = access.arm(run)
    e = economy(run)
    genres = {g: dict(professional=a['genres'][g]['issueSources'].get('ExternalProfessional', 0),
                      ratio=ratio(a['genres'][g]['issueSources'])) for g in GENRES if g in a['genres']}
    return dict(run=run, matureIntentions=a['matureIntentions'], genres=genres, arrivals=arrivals(run),
                demos=demo_destinations(run), albumCutSources=album_cut_sources(run), chart=professional_chart(run),
                singleLaunches=e['singleLaunches'], releasedAlbumProjects=e['releasedAlbumProjects'],
                closures=sum(e['exitReasons'].values()), exitReasons=e['exitReasons'],
                writingFees=a['workflow']['totalWritingFees'],
                careers={k: a['workflow'][k] for k in ('hitActs', 'top40Entries', 'middleRate', 'fixedTop30Share', 'leaderSlotShare')})


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--control-tag', default='writing-balance-off')
    p.add_argument('--treatment-tags', nargs='+', default=['writing-balance-filler', 'writing-balance-cap2', 'writing-balance-both'])
    p.add_argument('--seeds', type=int, nargs='+', default=[1001])
    p.add_argument('--output', default=str(access.CAL / 'writing-professional-song-balance.json'))
    args = p.parse_args()
    result = dict(control=args.control_tag, seeds={})
    for seed in args.seeds:
        control = f'{args.control_tag}-market-{seed}'
        check_complete(control)
        rows = {args.control_tag: dict(flags=[], **arm(control))}
        for tag in args.treatment_tags:
            run = f'{tag}-market-{seed}'
            check_complete(run)
            rows[tag] = dict(flags=validate(manifest(control), manifest(run)), **arm(run))
        result['seeds'][seed] = rows
    with open(args.output, 'w', encoding='utf-8') as f:
        json.dump(result, f, indent=2)
    for seed, rows in result['seeds'].items():
        print(f'seed {seed}')
        for tag, r in rows.items():
            m = r['matureIntentions']
            print(f'  {tag:30} pro {m["professional"]:5} covers {m["covers"]:5} ratio {m["coversPerProfessional"]:.2f} '
                  f'singles {m["singles"]:5} | arrivals {r["arrivals"]} | demos {r["demos"]} | LPs {r["releasedAlbumProjects"]} '
                  f'closures {r["closures"]} fees ${r["writingFees"]:,.0f}')
            print(f'  {"":30} genres { {g: (v["professional"], v["ratio"]) for g, v in r["genres"].items()} }')
            print(f'  {"":30} chart {r["chart"]} careers {r["careers"]}')


if __name__ == '__main__':
    main()
