import csv
import json
import tempfile
import unittest
from pathlib import Path

from fit_stature_bands import fit
from fit_stature_experiment import is_fit
from stature_normalization import COMPONENTS
from stature_shadow_policy import BANDS, validate


class BandFitChecks(unittest.TestCase):
    def test_training_partition_and_monotone_boundaries(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            components, regional, output = (root / x for x in ('components.csv', 'regional.csv', 'fit.json'))
            artists = [f'artist{i}' for i in range(2000) if is_fit(f'artist{i}')][:120]
            with components.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=['date', 'artistId', 'careerState', 'component', 'peerScore'])
                writer.writeheader()
                for index, artist in enumerate(artists):
                    for component in COMPONENTS:
                        writer.writerow({'date': '1960-01-01', 'artistId': artist, 'careerState': BANDS[index // 30],
                                         'component': component, 'peerScore': str(.1 + .2 * (index // 30))})
            with regional.open('w', newline='') as stream:
                fields = ['date', 'artistId', 'regionalScore', 'regionalDraw', 'regionalReach', 'recordReach', 'conversionReach']
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                for artist in artists:
                    index = artists.index(artist)
                    row = dict.fromkeys(fields, str(.1 + .2 * (index // 30)))
                    row.update(date='1960-01-01', artistId=artist, regionalReach='1', recordReach='1', conversionReach='1')
                    writer.writerow(row)
            fit(components, regional, output, '1960-01-08')
            result = json.loads(output.read_text())
            self.assertEqual(result['status'], 'candidate_only')
            self.assertEqual(result['counts'], dict.fromkeys(BANDS, 30))
            validate(result['calibration'])
            # Saturated evidence cannot identify distinct boundaries.
            rows = []
            with regional.open(newline='') as stream:
                rows = list(csv.DictReader(stream))
            with regional.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                for row in rows:
                    row['regionalScore'] = '1'; row['regionalDraw'] = '1'
                    writer.writerow(row)
            collapsed = root / 'collapsed.json'
            fit(components, regional, collapsed, '1960-01-08')
            result = json.loads(collapsed.read_text())
            self.assertEqual(result['status'], 'degenerate_overlapping_boundaries')
            self.assertNotIn('calibration', result)


if __name__ == '__main__':
    unittest.main()
