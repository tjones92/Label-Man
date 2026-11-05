import csv
import datetime as dt
import json
import tempfile
import unittest
from pathlib import Path

from fit_stature_experiment import is_fit
from screen_stature_programme_choices import BASE, TABLE, screen


class ChoiceScreenChecks(unittest.TestCase):
    def test_previous_week_only_and_below_rising_unmapped(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            choices, shadow, output, summary = (root / x for x in ('choices.csv', 'shadow.csv', 'out.csv', 'summary.json'))
            artist = next(f'a{i}' for i in range(1000) if not is_fit(f'a{i}'))
            fields = ['format', 'week', 'artistId', 'careerState', 'eligible', 'selected', 'labelId', 'tier', 'expectedNet', 'qualityEstimate', 'slotId']
            with choices.open('w', newline='') as stream:
                w = csv.DictWriter(stream, fieldnames=fields); w.writeheader()
                for week in (2, 3):
                    w.writerow(dict.fromkeys(fields, '') | dict(format='Album', week=week, artistId=artist, careerState='Rising', eligible='1'))
                w.writerow(dict.fromkeys(fields, '') | dict(format='Album', week=3, artistId=artist, careerState='NewSigning', eligible='1'))
            with shadow.open('w', newline='') as stream:
                w = csv.DictWriter(stream, fieldnames=['date', 'artistId', 'band', 'status']); w.writeheader()
                w.writerow(dict(date='1960-01-01', artistId=artist, band='Star', status='observed'))
                w.writerow(dict(date='1960-01-08', artistId=artist, band='', status='below_rising'))
            screen(choices, shadow, output, summary, dt.date(1960, 1, 1))
            with output.open(newline='') as stream:
                rows = list(csv.DictReader(stream))
            self.assertEqual(rows[0]['snapshotDate'], '1960-01-01')
            self.assertEqual(float(rows[0]['candidateMultiplier']), TABLE['Star'])
            self.assertEqual(float(rows[0]['appliedMultiplier']), TABLE['Rising'])
            self.assertEqual(rows[1]['reason'], 'below_rising_mapping_unresolved')
            self.assertEqual(rows[1]['candidateMultiplier'], '')
            self.assertEqual(rows[2]['reason'], 'below_rising_preserved_newsigning')
            self.assertEqual(float(rows[2]['candidateMultiplier']), BASE)
            self.assertEqual(json.loads(summary.read_text())['counts']['comparable'], 2)


if __name__ == '__main__':
    unittest.main()
