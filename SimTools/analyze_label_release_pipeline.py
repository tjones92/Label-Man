"""Trace recorded label intentions through preparation, issue and chart entry.

Reads existing CSVs only. No simulation or counterfactual hit assignment.
Repeated gate encounters are reported separately from distinct intentions.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_head_hit_conversion import require_complete_chart


def analyze(prefix, output, through=260):
    directory = pd.read_csv(f'{prefix}-label-directory.csv')
    wanted = ['Columbia', 'RCA Victor', 'Decca', 'MGM', 'ABC-Paramount', 'Warner Bros.', 'United Artists', 'Dot']
    labels = {r.labelId: r.labelName for r in directory.itertuples()
              if r.isHistorical and any(r.labelName == n or r.labelName.startswith(n+' ') for n in wanted)}
    assert len(labels) == len(wanted), 'Need unique historical head-label identities'
    output.mkdir(parents=True, exist_ok=True)

    def read(stream, columns=None):
        chunks = []
        for frame in pd.read_csv(f'{prefix}-{stream}.csv', usecols=columns, chunksize=100000):
            keep = frame[frame.labelId.isin(labels) & frame.week.le(through)].copy()
            if len(keep):
                keep['labelName'] = keep.labelId.map(labels)
                chunks.append(keep)
        if not chunks:
            return pd.DataFrame()
        return pd.concat(chunks, ignore_index=True)

    slate = read('release-slate')
    # Source append order resolves multiple reviews of the same slot in one week.
    last = slate.drop_duplicates('slotId', keep='last')
    mature = last[last.dueWeek.le(through)].copy()
    funnel = mature.groupby(['labelId', 'labelName', 'format']).agg(
        intentions=('slotId', 'size'), prepared=('preparedWeek', lambda x: x.notna().sum()),
        issued=('state', lambda x: x.eq('Issued').sum()),
        cancelled=('state', lambda x: x.eq('Cancelled').sum()))
    assert (funnel.issued <= funnel.prepared).all() and (funnel.prepared <= funnel.intentions).all()
    funnel['issueFraction'] = funnel.issued/funnel.intentions
    funnel.to_csv(output/'intention-funnel.csv')
    mature.to_csv(output/'mature-intentions.csv', index=False)
    mature.groupby(['labelId','labelName','format','state','reason']).size().rename('intentions').to_csv(output/'final-reasons.csv')

    resources = read('programme-resources')
    scale = resources.groupby(['labelId','labelName']).agg(
        observedWeeks=('week','nunique'), singleWeeklyRate=('singleWeeklyRate','mean'),
        albumWeeklyRate=('albumWeeklyRate','mean'), attempts=('attempts','sum'), successes=('successes','sum'),
        dueActWeeks=('dueFollowUps','sum'), unplannedDueActWeeks=('unplannedFollowUps','sum'))
    scale['annualSingleOpportunityRate'] = scale.singleWeeklyRate*52
    scale['annualAlbumOpportunityRate'] = scale.albumWeeklyRate*52
    scale.to_csv(output/'operating-opportunities.csv')

    refusals = read('programme-issue-refusals')
    refusals.groupby(['labelId','labelName','format','gate']).agg(
        encounters=('slotId','size'), intentions=('slotId','nunique')).to_csv(output/'issue-gates.csv')
    cash = read('programme-cash-shortfalls')
    if len(cash):
        cash.groupby(['labelId','labelName','format']).agg(encounters=('slotId','size'),
            intentions=('slotId','nunique'), medianCash=('cash','median'),
            medianTaxAndService=('taxAndService','median')).to_csv(output/'cash-gates.csv')
    transitions = read('label-tier-transitions')
    transitions.to_csv(output/'tier-transitions.csv',index=False)
    targets = read('label-operating-target-events')
    targets.to_csv(output/'sparse-roster-observations.csv',index=False)

    quality = read('single-launch-quality')
    assert not quality.singleId.duplicated().any()
    quality.groupby(['labelId','labelName','strategy']).size().rename('launches').to_csv(output/'single-strategies.csv')
    chart = pd.read_csv(f'{prefix}-records.csv', usecols=['week','recordId','artistId','currentPosition'])
    observed = int(chart.week.max())
    require_complete_chart(chart, observed)
    quality = quality[quality.week+12 <= observed].copy()
    first = chart[chart.currentPosition.between(1,10)].groupby('artistId').week.min()
    quality['priorObservedHit'] = quality.artistId.map(first) < quality.week
    observations = chart.merge(quality[['singleId','week']].rename(columns={'week':'launchWeek'}),
        left_on='recordId',right_on='singleId',validate='many_to_one')
    observations = observations[observations.week.between(observations.launchWeek,observations.launchWeek+12)]
    peak = observations.groupby('singleId').currentPosition.min()
    quality['entry'] = quality.singleId.map(peak).between(1,100)
    quality['hit'] = quality.singleId.map(peak).between(1,10)
    chart_funnel = quality.groupby(['labelId','labelName','priorObservedHit']).agg(
        launches=('singleId','size'),artists=('artistId','nunique'),entries=('entry','sum'),
        hits=('hit','sum'),meanQuality=('runtimeQuality','mean'))
    chart_funnel['entryRate'] = chart_funnel.entries/chart_funnel.launches
    chart_funnel['conversionGivenEntry'] = chart_funnel.hits/chart_funnel.entries.replace(0,float('nan'))
    chart_funnel.to_csv(output/'launch-funnel.csv')

    method = dict(simulationsStarted=0, prefix=str(prefix), throughWeek=through, chartWeeks=observed,
        labels=labels, assumptions=[
            'Intentions are distinct slot IDs, final state in source append order; due dates beyond the cutoff are excluded.',
            'Observed weekly opportunity rates are not the dated rates at every earlier planning review.',
            'Cash/issue gate counts overlap; retries do not equal prevented releases.',
            'Launch outcomes require full 13-week follow-up; known-hit status uses only earlier observed Top 10 weeks.',
            'Roster target events are sparse selected observations, not a weekly roster census.',
            'Fanbase rows omit artists with fewer than one attached audience equivalent and must not be used as full rosters.',
            'No slot expansion is assigned a hit, no existing chart is reranked, and no causal treatment effect is claimed.',
            'The retained input predates the shared LP purchasing policy; new-policy income and release feedback are unobserved.'])
    (output/'method.json').write_text(json.dumps(method,indent=2)+'\n')
    print(funnel.round(4).to_string())
    print('\nOperating opportunities:\n'+scale.round(3).to_string())
    print('\nLaunch-cohort funnel:\n'+chart_funnel.round(4).to_string())


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--prefix',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--through-week',type=int,default=260)
    args = parser.parse_args()
    analyze(args.prefix,args.output,args.through_week)
