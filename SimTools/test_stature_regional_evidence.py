import csv
import tempfile
import unittest
from pathlib import Path

from stature_normalization import SALES
from stature_regional_evidence import derive


class RegionalChecks(unittest.TestCase):
    def test_population_weighting_decay_and_missing(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, output = root / 'in.csv', root / 'out.csv'
            fields = ['date', 'artistId', 'regionId', 'regionPopulationMillions', 'followingStatus',
                      'followingYoung', 'followingAdult', 'singleAdvanceIntent', 'albumAdvanceIntent', *SALES,
                      *('market' + k[0].upper() + k[1:] for k in SALES)]
            with source.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields)
                writer.writeheader()
                for day in ('1/1/1960', '1/8/1960'):
                    for region, pop in [('small', 1), ('large', 3)]:
                        row = dict.fromkeys(fields, 0)
                        row.update(date=day, artistId='a', regionId=region, regionPopulationMillions=pop,
                                   followingStatus='observed', followingYoung=10 * pop,
                                   singleYoungUnits=10 if day == '1/1/1960' and region == 'small' else 0)
                        row.update({'market' + k[0].upper() + k[1:]: 100 for k in SALES})
                        writer.writerow(row)
            derive(source, output, {'version': 'test', 'effectiveDate': '1960-01-01', 'halfLifeWeeks': 1,
                'localSalesShareReference': .1, 'followingPerMillionReference': 100, 'marketFloorUnits': 1})
            with output.open(newline='') as stream:
                rows = list(csv.DictReader(stream))
            self.assertAlmostEqual(float(rows[0]['regionalScore']), 1)
            self.assertAlmostEqual(float(rows[0]['recordReach']), .325)
            self.assertAlmostEqual(float(rows[0]['conversionReach']), .1)
            self.assertAlmostEqual(float(rows[1]['regionalScore']), .5)
            self.assertAlmostEqual(float(rows[1]['recordReach']), .2)

    def test_missing_market_and_following_are_not_zero(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, output = root / 'in.csv', root / 'out.csv'
            fields = ['date', 'artistId', 'regionId', 'regionPopulationMillions', 'followingStatus',
                      'followingYoung', 'followingAdult', 'singleAdvanceIntent', 'albumAdvanceIntent', *SALES,
                      *('market' + k[0].upper() + k[1:] for k in SALES)]
            row = dict.fromkeys(fields, 0)
            row.update(date='1/1/1960', artistId='a', regionId='one', regionPopulationMillions=1,
                       followingStatus='no_following_record')
            with source.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader(); writer.writerow(row)
            derive(source, output, {'version': 'test', 'effectiveDate': '1960-01-01', 'halfLifeWeeks': 1,
                'localSalesShareReference': .1, 'followingPerMillionReference': 100, 'marketFloorUnits': 1})
            with output.open(newline='') as stream:
                result = next(csv.DictReader(stream))
            self.assertEqual(result['status'], 'missing_inputs')
            self.assertEqual(result['regionalScore'], '')
            self.assertEqual(result['regionalDraw'], '')
            self.assertEqual(result['recordReach'], '')
            self.assertEqual(result['followingStatus'], 'no_following_record')
            self.assertAlmostEqual(float(result['regionalDrawUpper']), .25)


if __name__ == '__main__':
    unittest.main()
