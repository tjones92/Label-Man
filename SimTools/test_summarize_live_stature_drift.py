import csv
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from summarize_live_stature_drift import COMPONENTS, summarize


class LiveDriftChecks(unittest.TestCase):
    def test_runtime_formation_does_not_use_future_event(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            shadow, evidence, population, output = (root / x for x in ('shadow.csv', 'evidence.csv', 'population.csv', 'out.json'))
            with population.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=('date', 'eventType', 'artistId'))
                writer.writeheader(); writer.writerow(dict(date='1/8/1960', eventType='formation', artistId='a'))
            with evidence.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=('date', 'artistId', 'labelTier', 'genre'))
                writer.writeheader()
                for date in ('1/1/1960', '1/8/1960'):
                    writer.writerow(dict(date=date, artistId='a', labelTier='Small', genre='Jazz'))
            with shadow.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=('date', 'artistId', 'status', 'shadowBand', *COMPONENTS))
                writer.writeheader()
                for date in ('1/1/1960', '1/8/1960'):
                    row = {c: '.5' for c in COMPONENTS}
                    row.update(date=date, artistId='a', status='Observed', shadowBand='Rising')
                    writer.writerow(row)
            with patch('summarize_live_stature_drift.verify'):
                summarize(shadow, evidence, population, output)
            groups = json.loads(output.read_text())['groups']
            self.assertEqual({g['formationCohort'] for g in groups}, {'not_observed_formed_since_start', 'observed_runtime_formation'})
            self.assertEqual(sum(g['counts']['artistWeeks'] for g in groups), 2)


if __name__ == '__main__':
    unittest.main()
