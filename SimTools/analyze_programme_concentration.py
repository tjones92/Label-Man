"""Decompose entry and slot-week concentration without changing running simulations.

Leaders are selected retrospectively within each three-year window. Release comparisons are descriptive,
not evidence of a causal effect of career standing, fan demand, or any individual profile switch.
"""
import argparse
import csv
import json
from pathlib import Path

import pandas as pd
from analyze_follower_calibration import load, entries, history

ROOT = Path(__file__).resolve().parents[1]
LOGS = ROOT / 'SimLogs'


def leaders(counts, fraction):
    return set(counts.sort_values(ascending=False).head(max(1, round(fraction*len(counts)))).index)


def names_for(run):
    names = {}
    with open(LOGS / f'{run}-following.csv', newline='', encoding='utf-8-sig') as f:
        for r in csv.DictReader(f):
            if r['artistId'] and r['artist']:
                names[r['artistId']] = r['artist']
    return names


def chart_decomposition(c, run, end_year, names, fixed_entry_n=None, fixed_slot_n=None):
    hits, _ = entries(c)
    hit_window = hits[hits.year.between(end_year-2,end_year)]
    observed = c[c.week.gt(0) & c.year.between(end_year-2,end_year)].copy()
    count_entries = hit_window.groupby('artistId').size()
    count_slots = observed.groupby('artistId').size()
    entry_leaders, slot_leaders = leaders(count_entries,.05), leaders(count_slots,.10)
    fixed_entry_n = fixed_entry_n or len(entry_leaders)
    fixed_slot_n = fixed_slot_n or len(slot_leaders)
    observed['simultaneousTitles'] = observed.groupby(['week','artistId']).recordId.transform('size')
    group = observed[observed.artistId.isin(slot_leaders)]
    others = observed[~observed.artistId.isin(slot_leaders)]
    bottom_ids = set(count_slots.index)-set(observed.loc[observed.currentPosition.le(40),'artistId'])
    bottom = observed[observed.artistId.isin(bottom_ids)]
    group_titles, all_titles = group.recordId.nunique(), observed.recordId.nunique()
    group_duration = len(group)/group_titles
    all_duration = len(observed)/all_titles
    slot_share = len(group)/len(observed)
    # Exact decomposition: title share x relative observed title duration = slot-week share.
    assert abs(slot_share - group_titles/all_titles * group_duration/all_duration) < 1e-10
    summary = dict(run=run, startYear=end_year-2, endYear=end_year,
        top40Entries=len(hit_window), hitActs=len(count_entries), entryLeaderActs=len(entry_leaders),
        oneEntryActsPct=100*count_entries.eq(1).mean(),twoToFourEntryActsPct=100*count_entries.between(2,4).mean(),
        fivePlusEntryActsPct=100*count_entries.ge(5).mean(),maxEntriesPerAct=int(count_entries.max()),
        entryLeaderEntries=int(count_entries.reindex(list(entry_leaders)).sum()),
        top5EntryPct=100*count_entries.reindex(list(entry_leaders)).sum()/len(hit_window),
        entriesPerEntryLeader=count_entries.reindex(list(entry_leaders)).mean(),
        entryLeaderObservedTitles=observed[observed.artistId.isin(entry_leaders)].recordId.nunique(),
        fixedEntryLeaderN=fixed_entry_n,
        fixedTopEntryPct=100*count_entries.sort_values(ascending=False).head(fixed_entry_n).sum()/len(hit_window),
        chartActs=len(count_slots), slotLeaderActs=len(slot_leaders),
        top40ObservedActs=observed.loc[observed.currentPosition.le(40),'artistId'].nunique(),
        bottomOnlyActs=len(bottom_ids),bottomOnlyObservedTitles=bottom.recordId.nunique(),
        bottomOnlySlotPct=100*len(bottom)/len(observed),
        bottomOnlyMeanObservedWeeks=len(bottom)/bottom.recordId.nunique() if len(bottom) else None,
        top10SlotPct=100*slot_share, slotLeaderObservedTitles=group_titles, allObservedTitles=all_titles,
        fixedSlotLeaderN=fixed_slot_n,
        fixedTopSlotPct=100*count_slots.sort_values(ascending=False).head(fixed_slot_n).sum()/len(observed),
        slotLeaderTitleSharePct=100*group_titles/all_titles, slotLeaderMeanObservedWeeks=group_duration,
        otherMeanObservedWeeks=len(others)/others.recordId.nunique(), allMeanObservedWeeks=all_duration,
        slotLeaderMultipleTitleSlotPct=100*group.simultaneousTitles.ge(2).mean(),
        allMultipleTitleSlotPct=100*observed.simultaneousTitles.ge(2).mean(),
        slotLeaderTop40SharePct=100*group.currentPosition.le(40).sum()/observed.currentPosition.le(40).sum(),
        slotLeaderBottom60SharePct=100*group.currentPosition.gt(40).sum()/observed.currentPosition.gt(40).sum(),
        leaderSetOverlap=len(entry_leaders & slot_leaders))
    actor_rows = []
    for definition, ids in [('entry5',entry_leaders),('slot10',slot_leaders)]:
        for act in ids:
            q = observed[observed.artistId.eq(act)]
            actor_rows.append(dict(run=run, definition=definition, artistId=act, artist=names.get(act,act),
                entries=int(count_entries.get(act,0)), slotWeeks=len(q), observedTitles=q.recordId.nunique(),
                observedWeeksPerTitle=len(q)/q.recordId.nunique(),
                multipleTitleSlotPct=100*q.simultaneousTitles.ge(2).mean()))
    return summary, actor_rows, entry_leaders, slot_leaders, observed, hits


def release_groups(c, hits, run, end_year, groups, tier=None):
    launches = pd.read_csv(LOGS / f'{run}-single-launch-quality.csv')
    start = int(c[c.year.ge(end_year-2)].week.min())
    end = min(int(c.week.max())-13, int(c[c.year.le(end_year)].week.max()))
    q = launches[launches.week.between(start,end)].copy()
    if tier:
        q = q[q.tier.eq(tier)].copy()
    first40 = c[c.currentPosition.le(40)].groupby('recordId').week.min()
    first100 = c.groupby('recordId').week.min()
    q['top40_13'] = q.singleId.map(first40).between(q.week,q.week+13)
    q['top100_13'] = q.singleId.map(first100).between(q.week,q.week+13)
    rows = []
    for definition, ids in groups.items():
        for member in (True,False):
            r = q[q.artistId.isin(ids).eq(member)]
            active = r.artistId.nunique()
            denominator_acts = len(ids) if member and not tier else active
            gaps = r.loc[r.weeksSinceLastSingle.ge(0),'weeksSinceLastSingle']
            rows.append(dict(run=run, definition=definition, tier=tier or 'All', group='leaders' if member else 'others',
                cohortActs=denominator_acts, releasingActs=active, releases=len(r),
                releasesPerActYear=len(r)/(denominator_acts*((end-start+1)/52)) if denominator_acts else None,
                top40Within13Pct=100*r.top40_13.mean(),top100Within13Pct=100*r.top100_13.mean(),
                meanQuality=r.runtimeQuality.mean(),medianObservedReleaseGap=gaps.median(),
                majorReleasePct=100*r.tier.eq('Major').mean(),starOrSuperstarReleasePct=100*r.careerState.isin(['Star','Superstar']).mean(),
                genreMix=r.genre.value_counts().to_dict()))
    return rows


def choice_groups(run, end_year, entry_leaders):
    result = {True:[],False:[]}
    with open(LOGS / f'{run}-programme-choices.csv',newline='',encoding='utf-8-sig') as f:
        for r in csv.DictReader(f):
            if r['format']!='Single' or r['selected']!='1' or not end_year-2<=int(r['intendedYear'])<=end_year:
                continue
            result[r['artistId'] in entry_leaders].append({k:float(r[k]) for k in
                ['gap','careerPriority','momentum','contribution','qualityEstimate','waitingPriority']})
    output=[]
    for member, rows in result.items():
        q=pd.DataFrame(rows)
        output.append(dict(run=run,group='entry5-leaders' if member else 'others',selectedIntentions=len(q),
            plannedGapUnder13Pct=100*q.gap.lt(13).mean(),**{k:float(q[k].mean()) for k in q.columns}))
    return output


def first_observed_hit_funnel(c, hits, run):
    """Fully observed post-hit outcomes for singles launched before established standing; descriptive cohorts."""
    first = hits.sort_values('week').drop_duplicates('artistId')
    first = first[first.week.between(5,int(c.week.max())-65) & first.launchCareerState.isin(['NewSigning','Rising'])]
    launches = pd.read_csv(LOGS / f'{run}-single-launch-quality.csv')
    launch_tiers = launches.drop_duplicates('singleId').set_index('singleId').tier.to_dict()
    first40 = c[c.currentPosition.le(40)].groupby('recordId').week.min()
    actors=[]
    for h in first.itertuples():
        q=launches[launches.artistId.eq(h.artistId) & launches.week.between(h.week+1,h.week+52)].copy()
        q['hit13']=q.singleId.map(first40).between(q.week,q.week+13)
        later=hits[hits.artistId.eq(h.artistId) & hits.week.between(h.week+1,h.week+52)]
        actors.append(dict(tier=launch_tiers.get(h.recordId,'Unknown'),majorOwnerAtHit=h.labelTier=='Major',
            nextMajorLaunch=None if q.empty else q.sort_values(['week','singleId']).iloc[0].tier=='Major',
            noRelease=int(q.empty),releases=len(q),
            firstReleaseGap=None if q.empty else int(q.week.min()-h.week),
            laterHits=len(later),followupHits13=int(q.hit13.sum())))
    if not actors:
        return []
    a=pd.DataFrame(actors)
    result=[]
    field=a[~a.tier.isin(['Major','Unknown'])]
    for tier,q in [('All',a),('Major',a[a.tier.eq('Major')]),('Field',field),
                   ('Field-major-owner-at-hit',field[field.majorOwnerAtHit]),
                   ('Field-field-owner-at-hit',field[~field.majorOwnerAtHit]),('Unknown',a[a.tier.eq('Unknown')])]:
        if q.empty:
            continue
        result.append(dict(run=run,originTier=tier,actors=len(q),noRelease52Pct=100*q.noRelease.mean(),
            majorOwnerAtFirstHitPct=100*q.majorOwnerAtHit.mean(),nextMajorLaunchPct=100*q.nextMajorLaunch.dropna().astype(float).mean(),
            meanReleases52=q.releases.mean(),medianFirstReleaseGap=q.firstReleaseGap.median(),
            noFurtherTop40Title52Pct=100*q.laterHits.eq(0).mean(),meanFurtherTop40Titles52=q.laterHits.mean(),
            releaseToTop40Within13Pct=100*q.followupHits13.sum()/q.releases.sum() if q.releases.sum() else None))
    return result


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--tag',default='followup-v4-fresh')
    ap.add_argument('--seeds',nargs='+',type=int,default=[1001,2002])
    ap.add_argument('--end-year',type=int,default=1963)
    args=ap.parse_args()
    out=LOGS/args.tag/'concentration'
    out.mkdir(parents=True,exist_ok=True)
    summaries,actors,releases,choices,frozen,major_releases,first_hits=[],[],[],[],[],[],[]
    h=history(); h=h[h.year.le(args.end_year)]
    summary, detail, *_=chart_decomposition(h,'history',args.end_year,{a:a for a in h.artistId.unique()})
    summaries.append(summary); actors.extend(detail)
    fixed_entry_n, fixed_slot_n=summary['entryLeaderActs'],summary['slotLeaderActs']
    for seed in args.seeds:
        saved={}
        for arm in ('control','treatment'):
            run=f'{args.tag}-{arm}-{seed}'
            c=load(run); names=names_for(run)
            summary,detail,e,s,observed,hits=chart_decomposition(c,run,args.end_year,names,fixed_entry_n,fixed_slot_n)
            summaries.append(summary);actors.extend(detail)
            releases.extend(release_groups(c,hits,run,args.end_year,dict(entry5=e,slot10=s)))
            major_releases.extend(release_groups(c,hits,run,args.end_year,dict(entry5=e,slot10=s),tier='Major'))
            choices.extend(choice_groups(run,args.end_year,e))
            first_hits.extend(first_observed_hit_funnel(c,hits,run))
            saved[arm]=dict(names=names,entries=e,slots=s,observed=observed,hits=hits)
        a,b=saved['control'],saved['treatment']
        for definition,key in [('entry5','entries'),('slot10','slots')]:
            ids={i for i in a[key] if i in a['names'] and a['names'].get(i)==b['names'].get(i)}
            row=dict(seed=seed,definition=definition,controlGroup=len(a[key]),matchedSameIdAndName=len(ids),dynamicGroupOverlap=len(a[key]&b[key]))
            for arm,data in saved.items():
                hits=data['hits']; hw=hits[hits.year.between(args.end_year-2,args.end_year)]
                row[arm+'EntrySharePct']=100*hw.artistId.isin(ids).mean()
                row[arm+'SlotSharePct']=100*data['observed'].artistId.isin(ids).mean()
            frozen.append(row)
    for name,rows in [('summary',summaries),('actors',actors),('release-groups',releases),('major-release-groups',major_releases),('choice-groups',choices),('frozen-groups',frozen),('first-observed-hit-funnel',first_hits)]:
        pd.DataFrame(rows).to_csv(out/f'{name}.csv',index=False)
    (out/'method.json').write_text(json.dumps(dict(seeds=args.seeds,years=[args.end_year-2,args.end_year],
        definitions='entry5: top 5% of actors by distinct new Top40 titles. slot10: top 10% by Top100 slot-weeks. Different ranked sets. Observed duration includes boundary censoring identically in history/model.',
        releaseWindow='Model launches within comparison years, excluding final 13 observation weeks for equal outcome opportunity. Historical full release catalogues are unavailable; no historical release-rate comparison is inferred.',
        firstHitFunnel='First observed new Top40 title per actor, launched NewSigning/Rising, first hit weeks 5..end-65. Origin tier is frozen launch tier when available, Unknown otherwise. Observe 52 weeks for later releases and 13 further weeks for their chart outcomes. This is not a claim about first-ever historical hits or uninterrupted career availability.',
        cautions='Retrospective leaders; association is not causal attribution. Frozen groups require same actor ID and reported name in both worlds; rank membership can change.'),indent=2),encoding='utf-8')
    print(pd.DataFrame(summaries).to_string(index=False))
    print(pd.DataFrame(releases).drop(columns='genreMix').to_string(index=False))
    print('Saved:',out)


if __name__=='__main__':
    main()
