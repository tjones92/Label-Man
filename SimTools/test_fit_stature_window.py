import csv
import json
import tempfile
import unittest
from pathlib import Path

from fit_stature_experiment import is_fit
from fit_stature_window import fit
from stature_normalization import SALES


class FitChecks(unittest.TestCase):
    def test_fit_excludes_holdout_and_future(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / 'in.csv'
            fields = ['date', 'artistId', 'regionId', 'regionPopulationMillions', 'followingStatus',
                      'followingYoung', 'followingAdult', 'tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate',
                      *SALES, *('market' + k[0].upper() + k[1:] for k in SALES)]
            fit_ids = [f'a{i}' for i in range(1000) if is_fit(f'a{i}')][:35]
            holdout = next(f'a{i}' for i in range(1000) if not is_fit(f'a{i}'))
            def write(other):
                with source.open('w', newline='') as stream:
                    writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                    for date in ('1/1/1960', '1/8/1960'):
                        for artist in fit_ids + [holdout]:
                            n = other if artist == holdout or date == '1/8/1960' else 1
                            row = dict.fromkeys(fields, n)
                            row.update(date=date, artistId=artist, regionId='r', regionPopulationMillions=1, followingStatus='observed')
                            row.update({'market' + k[0].upper() + k[1:]: 100000 for k in SALES})
                            writer.writerow(row)
            write(1)
            fit(source, root / 'one', 1)
            write(999)
            fit(source, root / 'two', 1)
            first = json.loads((root / 'one' / 'reference.json').read_text())
            second = json.loads((root / 'two' / 'reference.json').read_text())
            self.assertEqual(first, second)
            self.assertEqual(first['effectiveDate'], '1960-01-08')


if __name__ == '__main__':
    unittest.main()
