"""Read-only owner-group diagnosis of a completed national-audience screen.

Usage: python SimTools/analyze_label_balance.py --review SimLogs/afb-v2/review
       --output SimLogs/label-balance-v1

Reuses the screen's fixed 52-week hit cohorts, including silent acts. Annual
repeaters belong to the seller at the act's first Top 10 that year; follow-up
opportunities belong to the seller at the original hit, even after a move.
Nothing here reranks charts or estimates an individual causal treatment effect.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_head_hit_conversion import require_complete_chart
from analyze_hit_act_cadence import annual_metrics
from analyze_offchart_screen import ROOT, path
from analyze_studio_parent import BIG5, HISTORY_BIG5, HISTORY_STUDIO, studio_ids


GROUPS = ('big5', 'studio', 'field')
# A disclosed sensitivity, not a complete artist-authority file. Keep duets apart.
ALIASES = {
    'Elvis Presley With The Jordanaires': 'Elvis Presley',
    'Ray Charles And His Orchestra With The Raelettes': 'Ray Charles',
}


def owner_group(label, studios, historical=False):
    big = HISTORY_BIG5 if historical else BIG5
    return 'big5' if label in big else 'studio' if label in studios else 'field'


def annual_acts(chart):
    """One annual assignment per artist; count all that act's Top 10 records."""
    top = chart[chart.currentPosition.between(1, 10)].sort_values(
        ['week', 'currentPosition', 'recordId'])
    first = top.drop_duplicates(['year', 'artistId'])[
        ['year', 'artistId', 'group', 'labelId']]
    counts = top.groupby(['year', 'artistId']).recordId.nunique().rename('top10Records')
    return first.merge(counts, on=['year', 'artistId'], validate='one_to_one')


def event_count(rows, gap):
    """Anchored debut-date clusters, separated by owner, never a chained merge.

    This is a sensitivity proxy for two charted sides, not verified discography.
    gap=-1 retains every side, including simultaneous entries.
    """
    if gap < 0:
        return len(rows)
    count, anchor, owner = 0, None, None
    for row in rows.sort_values(['debut', 'recordId']).itertuples():
        if pd.isna(row.debut):
            raise ValueError('Historical Top 10 side has no Hot 100 observation')
        if anchor is None or row.labelId != owner or (row.debut - anchor).days > gap * 7:
            count += 1
            anchor, owner = row.debut, row.labelId
    return count


def history_sensitivity(years):
    chart = pd.read_csv(Path(ROOT) / 'historical-hot100-1960-64/hot100_weekly.csv',
                        parse_dates=['chartDate'])
    chart['year'] = chart.chartDate.dt.year
    chart['recordId'] = chart.artist + ' || ' + chart.title
    first = chart.groupby('recordId').chartDate.min()
    owners = pd.concat([pd.read_csv(Path(ROOT) / f'historical-top10-{y}/records_{y}_grouped.csv')
                       .assign(year=y) for y in years])
    chart = chart[chart.year.isin(years) & chart.position.le(10)].merge(
        owners[['year', 'artist', 'title', 'owned']], on=['year', 'artist', 'title'],
        how='left', validate='many_to_one')
    if chart.owned.isna().any():
        raise ValueError('Historical Top 10 owner missing')
    chart['labelId'] = chart.owned
    chart['group'] = chart.owned.map(lambda x: owner_group(x, HISTORY_STUDIO, True))
    chart['week'] = (chart.chartDate - chart.chartDate.min()).dt.days // 7
    chart['currentPosition'] = chart.position
    chart['debut'] = chart.recordId.map(first)
    rows, acts = [], []
    for alias in ('literal', 'limited-aliases'):
        chart['artistId'] = chart.artist.replace(ALIASES) if alias != 'literal' else chart.artist
        assigned = annual_acts(chart)
        sides = chart.drop_duplicates(['year', 'recordId'])
        for gap in (-1, 0, 1, 3):
            counts = sides.groupby(['year', 'artistId']).apply(
                lambda q: event_count(q, gap), include_groups=False).rename('events')
            panel = assigned.merge(counts, on=['year', 'artistId'], validate='one_to_one')
            acts.append(panel.assign(alias=alias, gapWeeks=gap))
            for (year, group), q in panel.groupby(['year', 'group']):
                rows.append(dict(year=year, group=group, alias=alias, gapWeeks=gap,
                                 acts=len(q), repeat2=int(q.events.ge(2).sum()),
                                 repeat3=int(q.events.ge(3).sum()), events=int(q.events.sum())))
    return pd.DataFrame(rows), pd.concat(acts, ignore_index=True)


def slot_outcomes(slate, through):
    """Unique early intentions, not repeated review rows or refusal attempts.

    An eight-week planning horizon plus eight-week expiry must fit before the
    endpoint. Actual assignment at the last observation owns the slot; an act
    reassignment does not create another unit of label work.
    """
    slate = slate[slate.week.le(through) & slate['format'].eq('Single')].copy()
    if slate.empty:
        raise ValueError('Missing release-slate observations')
    slate['_rowOrder'] = range(len(slate))
    # Preserve file order within a week: Proposed and Issued can share a week.
    slate = slate.sort_values(['week', '_rowOrder'])
    end = slate.drop_duplicates('slotId', keep='last').copy()
    end = end[end.proposedWeek.between(1, through - 16)]
    for column, reason in [('cashBlocked', 'insufficient-unreserved-cash'),
                           ('promotionBlocked', 'promotion-congestion'),
                           ('preparationBlocked', 'preparation-material-economics-or-funding')]:
        end[column] = end.slotId.isin(set(slate.loc[slate.reason.eq(reason), 'slotId']))
    end['issued'] = end.state.eq('Issued')
    end['cancelled'] = end.state.eq('Cancelled')
    end['windowExpired'] = end.reason.str.contains('window-expired', na=False)
    return end.drop(columns='_rowOrder')


def demand_panel(releases, stages, attributed, census=None):
    eligible = releases[releases.entry13Eligible.eq(1)].copy()
    if eligible.singleId.duplicated().any():
        raise ValueError('Duplicate eligible follow-up identity')
    launch = eligible[['singleId', 'week']].rename(columns={'week': 'launchWeek'})
    z = stages.merge(launch, left_on='recordId', right_on='singleId', validate='many_to_one')
    # Exactly 13 demand settlements, ages 0..12; entry13 remains the existing
    # screen's elapsed-time criterion (entry by launch+13).
    z = z[z.week.between(z.launchWeek, z.launchWeek + 12)]
    if z.duplicated(['recordId', 'week', 'region']).any():
        raise ValueError('Duplicate regional demand observation')
    per = z.groupby('recordId').agg(observedWeeks=('week', 'nunique'), raw=('rawDemand', 'sum'),
        serviceable=('serviceableDemand', 'sum'), cleared=('clearedUnits', 'sum'),
        fanCleared=('clearedFanUnits', 'sum'), fit=('commercialFitAtCapture', 'median'))
    eligible = eligible.merge(per, left_on='singleId', right_index=True, how='left', validate='one_to_one')
    expected = pd.Series(13, index=eligible.index)
    if census is not None:
        retired = census[census.status.eq('Retired')].set_index('recordId').retiredAuditWeek
        retirement = eligible.singleId.map(retired)
        expected = (retirement - eligible.week).clip(0, 13).fillna(13)
        if not expected.eq(expected.round()).all():
            raise ValueError('Invalid retirement week')
        stage_retirement = z.recordId.map(retired)
        if z.week.ge(stage_retirement).any():
            raise ValueError('Demand observed after recorded retirement')
    eligible['observedWeeks'] = eligible.observedWeeks.fillna(0)
    if not eligible.observedWeeks.eq(expected).all():
        raise ValueError('Need all 13 post-launch demand weeks, including misses')
    eligible['inactiveWeeks'] = 13 - expected
    # A retired record's subsequent weeks have no demand or purchases; its miss
    # remains in the denominator. Missing live weeks are never imputed as zero.
    for field in ['raw', 'serviceable', 'cleared', 'fanCleared']:
        eligible[field] = eligible[field].fillna(0)
    fields = ['singleId', 'initialStock', 'initialAwareness', 'effectiveRecognition', 'launchCampaignImpact']
    eligible = eligible.merge(attributed[fields], on='singleId', how='left', validate='one_to_one')
    if eligible[fields].isna().any().any():
        raise ValueError('Missing launch attribution')
    eligible['inventoryFulfillment'] = eligible.serviceable / eligible.raw.clip(lower=1)
    eligible['marketFulfillment'] = eligible.cleared / eligible.serviceable.clip(lower=1)
    return eligible


def validate_source(meta, arm, seed):
    source = meta['sources'][arm][str(seed)]
    tag = source['tag']
    manifest = json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig'))
    matches = [r for r in manifest['results'] if r['seed'] == seed and r['passed']]
    if len(matches) != 1 or manifest['weeks'] != source['sourceWeeks'] or manifest['weeks'] < meta['weeks']:
        raise ValueError(f'Need one completed native source: {arm}/{seed}')
    return tag, manifest['weeks'], manifest


def measure(meta, arm, seed, cohorts, releases):
    tag, weeks, manifest = validate_source(meta, arm, seed)
    through = meta['weeks']
    p = lambda stream: path(tag, seed, stream, weeks)
    studios = studio_ids(p)
    group = lambda s: s.map(lambda x: owner_group(x, studios))
    chart = pd.read_csv(p('records'))
    chart = chart[chart.week.le(through) & chart.currentPosition.between(1, 100)]
    require_complete_chart(chart, through)
    chart['group'] = group(chart.labelId)
    acts = annual_acts(chart)
    years = list(range(1960, 1960 + through // 52))
    acts = acts[acts.year.isin(years)]
    launches = pd.read_csv(p('single-launch-quality'))
    launches = launches[launches.week.le(through)].copy()
    launches['group'] = group(launches.labelId)
    attributed = pd.read_csv(p('single-launch-attribution'))
    stages = pd.read_csv(p('single-demand-stages'))
    if 'clearedFanUnits' not in stages:
        if any(x.startswith('--artist-fanbase=') for x in manifest['extraFlags']):
            raise ValueError('Audience source is missing returning-purchase attribution')
        stages['clearedFanUnits'] = 0
    panel = demand_panel(releases, stages, attributed, pd.read_csv(p('demand-release-census')))
    slots = slot_outcomes(pd.read_csv(p('release-slate')), through)
    slots['group'] = group(slots.labelId)
    finance = pd.read_csv(p('label-finance'))
    finance = finance[finance.week.le(through)].copy()
    finance['group'] = group(finance.labelId)
    short = pd.read_csv(p('programme-cash-shortfalls'))
    short = short[short.week.le(through) & short['format'].eq('Single')].copy()
    short['group'] = group(short.labelId)
    directory = pd.read_csv(p('label-directory')).set_index('labelId').labelName
    label_rows = []
    for label, z in slots.groupby('labelId'):
        if owner_group(label, studios) == 'field':
            continue
        l = launches[launches.labelId.eq(label)]
        t = chart[chart.labelId.eq(label) & chart.currentPosition.le(10) & chart.year.isin(years)]
        a = acts[acts.labelId.eq(label)]
        f = finance[finance.labelId.eq(label)]
        b = short[short.labelId.eq(label)]
        label_rows.append(dict(labelId=label, labelName=directory.get(label, label), group=owner_group(label, studios),
            launches=len(l), top10Slots=len(t), annualRepeatActs=int(a.top10Records.ge(2).sum()) / len(years),
            earlySlots=len(z), issuedSlots=int(z.issued.sum()), cashBlockedSlots=int(z.cashBlocked.sum()),
            promotionBlockedSlots=int(z.promotionBlocked.sum()), windowExpiredSlots=int(z.windowExpired.sum()),
            strugglingWeeks=int(f.status.eq('Struggling').sum()), cashShortSlots=int(b.slotId.nunique()),
            cashShortWeeks=int(b.week.nunique())))
    rows = []
    for g in GROUPS:
        c = cohorts[cohorts.group.eq(g)]
        r = releases[releases.group.eq(g)]
        d = panel[panel.group.eq(g)]
        s = slots[slots.group.eq(g)]
        f = finance[finance.group.eq(g)]
        rows.append(dict(group=g, acts=len(c), followups=int(c.releases52.sum()),
            releasesPerAct=float(c.releases52.mean()), noReleaseShare=float(c.noRelease52.mean()),
            releaseGap=float(r.loc[r.releaseGap.ge(0), 'releaseGap'].median()),
            under13GapShare=float(r.loc[r.releaseGap.ge(0), 'releaseGap'].lt(13).mean()),
            debutsPerAct=float(c.debuts52.mean()), top10PerEntry=float(c.top10Debuts52.sum() / max(1, c.debuts52.sum())),
            eligibleLaunches=len(d), entry13Share=float(d.entered13.mean()), top10_13Share=float(d.top10_13.mean()),
            quality=float(d.quality.mean()), initialStock=float(d.initialStock.median()),
            initialAwareness=float(d.initialAwareness.median()), campaign=float(d.launchCampaignImpact.median()),
            inventoryFulfillment=float(d.serviceable.sum() / max(1, d.raw.sum())),
            marketFulfillment=float(d.cleared.sum() / max(1, d.serviceable.sum())),
            returningUnitShare=float(d.fanCleared.sum() / max(1, d.cleared.sum())),
            earlySlots=len(s), issuedSlotShare=float(s.issued.mean()),
            cashBlockedSlotShare=float(s.cashBlocked.mean()), promotionBlockedSlotShare=float(s.promotionBlocked.mean()),
            windowExpiredSlotShare=float(s.windowExpired.mean()),
            strugglingLabelWeekShare=float(f.status.eq('Struggling').mean())))
    # Reconcile independently with the published annual owner-group numbers.
    empty_launches = pd.DataFrame(columns=['year', 'artistId', 'weeksSinceLastSingle'])
    annual = annual_metrics(chart, empty_launches, set(), years=years)
    for y in years:
        for g in GROUPS:
            q = acts[acts.year.eq(y) & acts.group.eq(g)]
            if int(q.top10Records.ge(2).sum()) != annual[y][f'{g}_repeat2']:
                raise ValueError('Annual artist ownership assignment does not reconcile')
    return (pd.DataFrame(rows), pd.DataFrame(label_rows), panel,
            dict(tag=tag, seed=seed, sourceWeeks=weeks, assemblySha256=manifest['assemblySha256']))


def markdown_table(frame):
    # Avoid an extra tabulate dependency.
    def cell(x):
        return f'{x:.3f}' if isinstance(x, float) else str(x)
    return '\n'.join(['| ' + ' | '.join(map(str, frame.columns)) + ' |',
                      '| ' + ' | '.join(['---'] * len(frame.columns)) + ' |',
                      *['| ' + ' | '.join(cell(x) for x in row) + ' |' for row in frame.itertuples(index=False, name=None)]])


def validate_pilot(plan, manifests):
    if plan['status'] != 'native-complete' or len(set(plan['seeds'])) != len(plan['seeds']):
        raise ValueError('Need a completed pilot with distinct seeds')
    if set(plan['arms']) != {'control', f"floor{plan['singleFloor']}"}:
        raise ValueError('Need the declared control and spacing arms')
    if set(manifests) != set(plan['arms']):
        raise ValueError('Need both pilot manifests')
    for arm, manifest in manifests.items():
        flags = plan['baseFlags'] + ([] if arm == 'control' else [f"--slate-single-floor={plan['singleFloor']}"])
        if (sorted(manifest['extraFlags']) != sorted(flags) or manifest['profile'] != 'game' or
            manifest['weeks'] != plan['weeks'] or manifest['assemblySha256'] != plan['assemblySha256']):
            raise ValueError('Pilot differs in more than the declared spacing floor')
        results = manifest['results']
        if (sorted(r['seed'] for r in results) != sorted(plan['seeds']) or
            not all(r['passed'] and r['exitCode'] == 0 for r in results)):
            raise ValueError('Incomplete or duplicate pilot seeds')


def pilot_review(plan_path, out):
    # Reuse the same complete-cohort and economic accounting calculations as the
    # audience screen, while validating this experiment's distinct intervention.
    from analyze_followup_chart_funnel import summary
    from analyze_hit_act_fit_support import measure as screen_measure

    plan = json.loads(Path(plan_path).read_text(encoding='utf-8-sig'))
    manifests = {arm: json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig'))
                 for arm, tag in plan['arms'].items()}
    validate_pilot(plan, manifests)
    out.mkdir(parents=True, exist_ok=True)
    meta = dict(runs=plan['arms'], weeks=plan['weeks'], commonCompletedSeeds=plan['seeds'], sources={})
    cohorts, releases, annual, guards, summaries, careers = [], [], [], [], [], []
    for arm, tag in plan['arms'].items():
        meta['sources'][arm] = {}
        for seed in plan['seeds']:
            meta['sources'][arm][str(seed)] = dict(tag=tag, sourceWeeks=plan['weeks'])
            c, r, career, a, g = screen_measure(tag, seed, plan['weeks'], plan['weeks'])
            cohorts.append(c.assign(arm=arm, seed=seed)); releases.append(r.assign(arm=arm, seed=seed))
            careers.append(career.assign(arm=arm, seed=seed))
            annual.extend(dict(arm=arm, seed=seed, year=y, **v) for y, v in a.items())
            guards.append(dict(arm=arm, seed=seed, **g))
            for group, q in c.groupby('group'):
                z = r[r.group.eq(group)]
                k = career[career.group.eq(group)]
                s = summary(q, z)
                s.update(activeUnownedWeeks=float(k.activeUnownedWeeks.mean()),
                    recognitionGain=float(k.recognitionChange.mean()),
                    belowEstablishedToEstablished=float(k.newEstablished.sum() / max(1, k.startedBelowEstablished.sum())))
                summaries.append(dict(arm=arm, seed=seed, group=group, **s))
    g = pd.DataFrame(guards)
    if g.groupby('seed')[['basePurchaseCapacity', 'albumChannelCapacity']].nunique().gt(1).any().any():
        raise ValueError('A spacing treatment changed the authored consumer purse')
    pd.concat(cohorts, ignore_index=True).to_json(out / 'cohorts.json', orient='records', indent=2)
    pd.concat(releases, ignore_index=True).to_csv(out / 'followup-releases.csv', index=False)
    pd.concat(careers, ignore_index=True).to_csv(out / 'career-progress.csv', index=False)
    pd.DataFrame(annual).to_csv(out / 'annual.csv', index=False)
    pd.DataFrame(summaries).to_csv(out / 'cohort-summary.csv', index=False)
    g.to_csv(out / 'guards.csv', index=False)
    (out / 'metadata.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    source = ap.add_mutually_exclusive_group(required=True)
    source.add_argument('--review')
    source.add_argument('--pilot-plan')
    ap.add_argument('--arms', nargs='+')
    ap.add_argument('--output', required=True)
    args = ap.parse_args()
    out = Path(args.output)
    review = pilot_review(args.pilot_plan, out / 'pilot-review') if args.pilot_plan else Path(args.review)
    meta = json.loads((review / 'metadata.json').read_text(encoding='utf-8'))
    arms = args.arms or (list(meta['runs']) if args.pilot_plan else ['blend'])
    cohorts = pd.read_json(review / 'cohorts.json')
    releases = pd.read_csv(review / 'followup-releases.csv')
    panels, labels, demand, sources = [], [], [], []
    for arm in arms:
        for seed in meta['commonCompletedSeeds']:
            c = cohorts[cohorts.arm.eq(arm) & cohorts.seed.eq(seed)]
            r = releases[releases.arm.eq(arm) & releases.seed.eq(seed)]
            if c.empty or c.duplicated('artistId').any() or not c.hitWeek.between(5, meta['weeks'] - 52).all():
                raise ValueError('Need unique complete hit cohorts, including silent acts')
            a, l, d, source = measure(meta, arm, seed, c, r)
            panels.append(a.assign(arm=arm, seed=seed)); labels.append(l.assign(arm=arm, seed=seed))
            demand.append(d.assign(arm=arm, seed=seed)); sources.append(dict(arm=arm, **source))
    out.mkdir(parents=True, exist_ok=True)
    panel = pd.concat(panels, ignore_index=True)
    panel.to_csv(out / 'opportunities.csv', index=False)
    pd.concat(labels, ignore_index=True).to_csv(out / 'labels.csv', index=False)
    pd.concat(demand, ignore_index=True).to_csv(out / 'demand.csv', index=False)
    annual = pd.read_csv(review / 'annual.csv')
    hist, hist_acts = history_sensitivity(sorted(annual.year.unique()))
    hist.to_csv(out / 'historical-sensitivity.csv', index=False)
    hist_acts.to_csv(out / 'historical-acts.csv', index=False)
    fields = ['acts', 'releasesPerAct', 'releaseGap', 'under13GapShare', 'entry13Share', 'top10PerEntry',
              'quality', 'initialStock', 'initialAwareness', 'campaign', 'inventoryFulfillment',
              'marketFulfillment', 'issuedSlotShare', 'cashBlockedSlotShare', 'windowExpiredSlotShare']
    means = panel.groupby(['arm', 'group'])[fields].mean().round(3).reset_index()
    history_means = hist.groupby(['alias', 'gapWeeks', 'group'])[['acts', 'repeat2', 'repeat3']].mean().reset_index()
    text = '# Label-balance observation ledger\n\n' + markdown_table(means) + '\n\n' + markdown_table(history_means)
    text += ('\n\nRates above are equal-weight means of seed metrics. Demand uses 13 weeks per eligible launch, '
             'including misses; recorded retirement permits zero demand only after retirement. Slot outcomes use '
             'unique single intentions proposed by week endpoint minus 16, '
             'with nonexclusive ever-blocked indicators. A blocked slot may later issue. Plans that were never '
             'created are outside that denominator. Historical gap=-1 retains sides; 0/1/3 use anchored debut '
             'clusters, with owner changes separated. First observations at the start of 1960 are left censored. '
             'The limited alias sensitivity merges only the two explicitly listed backing-credit variants. '
             'Neither sensitivity is a replacement release discography or adopted calibration target.\n')
    (out / 'observations.md').write_text(text, encoding='utf-8')
    (out / 'sources.json').write_text(json.dumps(dict(review=str(review), weeks=meta['weeks'], sources=sources,
        aliases=ALIASES, demandWeeks=13, slotTailWeeks=16), indent=2), encoding='utf-8')
    print(text)


if __name__ == '__main__':
    main()
