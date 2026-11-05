import csv
import tempfile
import unittest
from pathlib import Path

from stature_normalization import COMPONENTS
from stature_shadow_trace import run
from test_stature_shadow_policy import calibration


class TraceChecks(unittest.TestCase):
    def test_trace_preserves_unknown_and_resets_after_gap(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            components, reach, output = (root / n for n in ('components.csv', 'reach.csv', 'out.csv'))
            with components.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=['date', 'artistId', 'careerState', 'component', 'fixedScore'])
                writer.writeheader()
                for date, value in [('1960-01-01', '.9'), ('1960-01-08', '0'), ('1960-01-22', '.9')]:
                    for component in COMPONENTS:
                        writer.writerow(dict(date=date, artistId='a', careerState='NewSigning', component=component, fixedScore=value))
            with reach.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=['date', 'artistId', 'regionalScore', 'regionalDraw', 'regionalReach', 'recordReach', 'conversionReach'])
                writer.writeheader()
                for date in ('1960-01-01', '1960-01-08', '1960-01-22'):
                    writer.writerow(dict(date=date, artistId='a', regionalReach='1', recordReach='1', conversionReach='1'))
            run(components, reach, calibration(), 'fixedScore', output)
            with output.open(newline='') as stream:
                rows = list(csv.DictReader(stream))
            self.assertEqual(rows[0]['band'], 'Superstar')
            self.assertEqual(rows[1]['status'], 'missing_evidence')
            self.assertEqual(rows[1]['previousBand'], 'Superstar')
            self.assertEqual(rows[2]['previousBand'], '')

    def test_explicit_pruned_bound_can_resolve_quiet_week(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            components, reach, output = (root / n for n in ('components.csv', 'reach.csv', 'out.csv'))
            fields = ['date', 'artistId', 'careerState', 'component', 'peerScore', 'peerReference']
            with components.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                for component in COMPONENTS:
                    writer.writerow(dict(date='1960-01-08', artistId='a', careerState='NewSigning', component=component,
                        peerScore='' if component.startswith('following_') else '0', peerReference='1000'))
            fields = ['date', 'artistId', 'regionalScore', 'regionalDraw', 'regionalDrawUpper', 'regionalReach',
                      'recordReach', 'recordReachLower', 'recordReachUpper', 'conversionReach', 'conversionReachUpper', 'followingStatus']
            with reach.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                writer.writerow(dict(date='1960-01-08', artistId='a', regionalScore='0', regionalDraw='',
                    regionalDrawUpper='.025', regionalReach='', recordReach='', recordReachLower='0', recordReachUpper='.025',
                    conversionReach='', conversionReachUpper='.025', followingStatus='no_following_record'))
            run(components, reach, calibration(), 'peerScore', output, bound_pruned=True)
            with output.open(newline='') as stream:
                self.assertEqual(next(csv.DictReader(stream))['status'], 'below_rising')


if __name__ == '__main__':
    unittest.main()
