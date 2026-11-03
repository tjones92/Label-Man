"""Observe fresh professional material versus covers; never infer writer ability from releases.

Uses mature single intentions (26 weeks), actual launch provenance and launch genre.
Search events are deduplicated by intention. No causal or non-writer classification
is possible with these traces: ability and rejected candidate offers were not saved.
"""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path

from analyze_writing_concentration import iter_rows, LOGS


def summarize(run, expected_weeks=104):
    launches = {r['recordId']: r for r in iter_rows(run, 'writing-launches')}
    quality = {r['singleId']: r for r in iter_rows(run, 'single-launch-quality')}
    end = max(int(r['week']) for r in iter_rows(run, 'records'))
    if end != expected_weeks:
        raise ValueError(f'{run}: expected {expected_weeks} weeks')
    plans = {}
    for r in iter_rows(run, 'release-slate'):
        proposed = int(r['proposedWeek'])
        if r['format'] != 'Single' or proposed < 1 or proposed + 26 > end or int(r['week']) > proposed + 26:
            continue
        p = plans.setdefault(r['slotId'], dict(proposed=proposed, career='Unknown', searches=[], record=None))
        for e in r['evidence'].split(';'):
            if e.startswith('career='):
                p['career'] = e.split('=', 1)[1]
        if r['state'] == 'Issued' and r['actualWeek'] and int(r['actualWeek']) <= proposed + 26:
            p['record'] = r['recordId']
    for r in iter_rows(run, 'programme-material-search'):
        p = plans.get(r['slotId'])
        if p and int(r['week']) <= p['proposed'] + 26:
            p['searches'].append(r)
    groups = defaultdict(list)
    genre_groups = defaultdict(list)
    for p in plans.values():
        groups['all'].append(p)
        groups[p['career']].append(p)
        if p['record']:
            if p['record'] not in launches or p['record'] not in quality:
                raise ValueError(f'{run}: missing issued provenance')
            p['launch'] = launches[p['record']]
            genre_groups[quality[p['record']]['genre']].append(p)

    def describe(ps):
        issued = [p for p in ps if p['record']]
        sources = Counter(p['launch']['source'] for p in issued)
        source_hooks = {}
        for source in sources:
            xs = [float(p['launch']['recordHook']) for p in issued if p['launch']['source'] == source]
            source_hooks[source] = sum(xs) / len(xs)
        transitions = Counter()
        for p in ps:
            events = p['searches']
            for key, predicate in [
                ('searchObserved', lambda r: True),
                ('declinedNoWorthwhileOffer', lambda r: r['state'] == 'Declined'),
                ('professionalStarted', lambda r: r['professional'] == '1' and r['state'] == 'Pending'),
                ('coverStarted', lambda r: r['professional'] == '0' and r['state'] == 'Pending'),
                ('professionalReady', lambda r: r['professional'] == '1' and r['state'] == 'Ready'),
                ('professionalDisappointed', lambda r: r['professional'] == '1' and r['reason'] == 'delivered-material-disappoints'),
                ('professionalConsumed', lambda r: r['professional'] == '1' and r['state'] == 'Consumed')]:
                transitions[key] += any(predicate(r) for r in events)
        return dict(plans=len(ps), issued=len(issued), issueSources=dict(sources), meanRecordHookBySource=source_hooks,
                    searchPlanCounts=dict(transitions))
    health = list(iter_rows(run, 'repertoire-health'))
    return dict(run=run, weeks=end, careers={k: describe(v) for k, v in sorted(groups.items())},
                issuedGenres={k: describe(v) for k, v in sorted(genre_groups.items())},
                repertoireArrivals={k: sum(int(r[k]) for r in health) for k in
                    ('arrivalsArtistWritten', 'arrivalsProfessional', 'arrivalsStandard', 'arrivalsRecentHit', 'arrivalsTraditional')},
                limitations=['No artist writing-ability or craft census; non-writers cannot be identified.',
                    'No rejected-offer trace; cash, rights, genre, fit and sampling losses cannot be counted separately.',
                    'Search Declined combines no candidate, unsuitable candidate and insufficient incremental value.',
                    'Genre is observed at launch; genre tables include issued plans only.',
                    'Arrival totals include initialization and all observed weeks; they are not unique songs or mature plans.'])


if __name__ == '__main__':
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('runs', nargs='+')
    ap.add_argument('--output', type=Path, required=True)
    ap.add_argument('--weeks', type=int, default=104)
    args = ap.parse_args()
    manifests = [json.loads((LOGS / 'writing-calibration' / f'{r}-manifest.json').read_text(encoding='utf-8-sig')) for r in args.runs]
    if len({m['assemblySha256'] for m in manifests}) != 1:
        raise ValueError('Runs must share a frozen assembly')
    result = [summarize(r, args.weeks) for r in args.runs]
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
