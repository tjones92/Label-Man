import csv
import json
import tempfile
import unittest
from pathlib import Path

from compare_stature_follower_shadow import compare


class FollowerShadowChecks(unittest.TestCase):
    def test_counts_only_consecutive_comparable_rises(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline, sensitivity, output = (root / name for name in ('baseline.csv', 'sensitivity.csv', 'out.json'))
            for path, bands in ((baseline, ['', 'Rising', 'Star', 'Star']),
                                (sensitivity, ['', '', 'Rising', 'Star'])):
                with path.open('w', newline='') as stream:
                    writer = csv.DictWriter(stream, fieldnames=('date', 'artistId', 'status', 'band'))
                    writer.writeheader()
                    for day, band, status in zip((1, 8, 15, 29), bands, ('below_rising', 'observed', 'missing_evidence', 'observed')):
                        writer.writerow(dict(date=f'1960-01-{day:02d}', artistId='a', status=status, band=band))
            compare(baseline, sensitivity, output)
            counts = json.loads(output.read_text())['counts']
            self.assertEqual(counts['observations'], 4)
            self.assertEqual(counts['differentBand'], 1)
            self.assertEqual(counts['baselineOnlyRise'], 1)
            self.assertEqual(counts['sensitivityOnlyRise'], 0)


if __name__ == '__main__':
    unittest.main()
