import csv
import datetime as dt
import json
import tempfile
import unittest
from pathlib import Path

from screen_stature_chart_transitions import scan


class ChartTransitionChecks(unittest.TestCase):
    def test_counts_entries_and_only_consecutive_observed_transitions(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            records, shadow, output = (root / n for n in ('records.csv', 'shadow.csv', 'out.json'))
            with records.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=['artistId', 'currentPosition', 'previousPosition', 'week'])
                writer.writeheader()
                writer.writerow(dict(artistId='a', currentPosition=35, previousPosition=50, week=2))
                writer.writerow(dict(artistId='a', currentPosition=20, previousPosition=35, week=3))
            with shadow.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=['artistId', 'date', 'band', 'status']); writer.writeheader()
                for date, band in [('1960-01-01', ''), ('1960-01-08', 'Rising'), ('1960-01-22', 'Star')]:
                    writer.writerow(dict(artistId='a', date=date, band=band, status='observed'))
            scan(records, shadow, output, dt.date(1960, 1, 1))
            result = json.loads(output.read_text())
            self.assertEqual(result['transitions'], 1)
            self.assertEqual(result['sameWeekTop40Entry'], 1)
            self.assertEqual(result['observedArtistWeeksWithTop40Entry'], 1)


if __name__ == '__main__':
    unittest.main()
