"""Attach programme opportunities and audit rows to selected live trajectories.

These are observed events, not causal attributions to a standing transition.
Programme week numbers are preserved directly from their source files.
"""
import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path

from stature_normalization import file_digest


def enrich(sample, decisions, slate, output, launches=None, campaigns=None):
    if (launches is None) != (campaigns is None):
        raise ValueError('Supply both launch attribution and campaign costs')
    data = json.loads(sample.read_text(encoding='utf-8-sig'))
    selected = {row['artistId'] for row in data['selected'].values()}
    attached = defaultdict(lambda: {'albumCandidates': [], 'releaseSlateEvents': [], 'releaseCampaignCosts': []})
    record_artists = {}
    def bind(record, artist):
        if not record or not artist:
            return
        if record in record_artists and record_artists[record] != artist:
            raise ValueError('Record linked to multiple selected artists')
        record_artists[record] = artist
    with decisions.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if row['artistId'] in selected:
                attached[row['artistId']]['albumCandidates'].append({key: row[key] for key in
                    ('date', 'week', 'labelId', 'tier', 'slotId', 'eligible', 'selected', 'careerState',
                     'snapshotDate', 'shadowBand', 'readStatus', 'oldMultiplier', 'proposedMultiplier', 'appliedMultiplier', 'expectedNet')})
    with slate.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if row['artistId'] in selected:
                bind(row['recordId'], row['artistId'])
                attached[row['artistId']]['releaseSlateEvents'].append({key: row[key] for key in
                    ('week', 'slotId', 'labelId', 'format', 'state', 'source', 'intendedWeek', 'actualWeek',
                     'intendedDate', 'actualDate', 'recordId', 'reservedProduction', 'reservedMarketing',
                     'reservedPressing', 'paidProduction', 'reason')})
    if launches is not None:
        with launches.open(newline='', encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream):
                if row['artistId'] in selected:
                    bind(row['singleId'], row['artistId'])
        with campaigns.open(newline='', encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream):
                artist = record_artists.get(row['recordId'])
                if artist:
                    attached[artist]['releaseCampaignCosts'].append({key: row[key] for key in
                        ('week', 'recordId', 'labelId', 'productionCost', 'marketingCost', 'releaseTimeExpectedNet')})
    result = {'sampleSha256': file_digest(sample), 'decisionSha256': file_digest(decisions), 'slateSha256': file_digest(slate),
              'launchesSha256': file_digest(launches) if launches else None, 'campaignsSha256': file_digest(campaigns) if campaigns else None,
              'selected': data['selected'], 'unobservedCategories': data['unobservedCategories'],
              'programmeByArtist': dict(attached),
              'caveat': 'Event rows include ineligible candidates and non-release states. Reservations are not realized spend. Campaign costs are attached only for records linked through selected slate or single-launch rows and do not cover every later tranche. They do not identify exposure, retained TV conversions, complete chart milestones, or causal effects. Weekly evidence and scores remain in the source sample.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('sample', type=Path)
    p.add_argument('decisions', type=Path)
    p.add_argument('slate', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--launches', type=Path); p.add_argument('--campaigns', type=Path)
    a = p.parse_args()
    enrich(a.sample, a.decisions, a.slate, a.output, a.launches, a.campaigns)
