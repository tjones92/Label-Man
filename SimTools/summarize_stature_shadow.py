"""Summarize held-out weekly shadow bands and legacy-rung cross-tabs."""
import argparse
import csv
import json
from collections import Counter
from pathlib import Path

from stature_normalization import file_digest
from stature_shadow_policy import BANDS


def summarize(source, output):
    statuses, bands, cross, routes, transitions = Counter(), Counter(), Counter(), Counter(), Counter()
    observed, mismatch = 0, 0
    with source.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            status = row['status']; statuses[status] += 1
            if status == 'missing_evidence':
                continue
            band = row['band'] or 'BelowRising'
            legacy = row['careerState']
            bands[band] += 1; cross[(legacy, band)] += 1
            if legacy in BANDS:
                observed += 1
                mismatch += legacy != band
            if row['band']:
                routes[row[row['band'] + 'LeadingRoute'] or 'none'] += 1
            previous = row['previousBand'] or 'BelowRising'
            if previous != band:
                transitions[(previous, band)] += 1
    data = {'sourceSha256': file_digest(source), 'statuses': dict(statuses), 'bands': dict(bands),
            'careerCrossTab': [{'legacy': k[0], 'shadow': k[1], 'count': v} for k, v in sorted(cross.items())],
            'legacyBandComparableRows': observed, 'legacyBandMismatchRows': mismatch,
            'legacyBandMismatchFraction': mismatch / observed if observed else None,
            'leadingRoutes': dict(routes), 'transitions': [{'from': k[0], 'to': k[1], 'count': v} for k, v in sorted(transitions.items())],
            'caveats': 'Weekly observations, not distinct decisions; initial states count as transitions; unknown weeks omitted; no causal inference.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(data, stream, indent=2, sort_keys=True); stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); summarize(a.source, a.output)
