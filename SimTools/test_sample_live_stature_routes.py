import csv
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from sample_live_stature_routes import MEASURES, sample


class LiveRouteChecks(unittest.TestCase):
    def test_roster_transition_and_adult_lp_response(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            shadow, evidence, output = root / 'shadow.csv', root / 'evidence.csv', root / 'out.json'
            scores = ['singleYoungScore', 'singleAdultScore', 'albumYoungScore', 'albumAdultScore',
                      'followingYoungScore', 'followingAdultScore', 'tvYoungScore', 'tvAdultScore']
            fields = ['date', 'artistId', 'careerState', 'status', 'shadowBand', 'RisingRegional', 'RisingRecord', 'RisingConversion', *scores]
            with shadow.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields)
                writer.writeheader()
                for date, band in [('1/1/1960', 'BelowRising'), ('1/8/1960', 'Rising')]:
                    row = {field: '0.1' for field in scores}
                    row.update(date=date, artistId='a', careerState='NewSigning', status='Observed', shadowBand=band,
                               RisingRegional='.2', RisingRecord='.8', RisingConversion='.1', albumAdultScore='.7')
                    writer.writerow(row)
            fields = ['date', 'artistId', 'artist', 'labelId', 'labelTier', 'genre', 'followingStatus', *MEASURES]
            with evidence.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields)
                writer.writeheader()
                for date in ('1/1/1960', '1/8/1960'):
                    row = {field: '0' for field in MEASURES}
                    row.update(date=date, artistId='a', artist='A', labelId='l', labelTier='Small', genre='Jazz', followingStatus='observed')
                    writer.writerow(row)
            with patch('sample_live_stature_routes.verify'):
                sample(shadow, evidence, output)
            result = json.loads(output.read_text())
            self.assertEqual(result['selected']['record_rise']['artistId'], 'a')
            self.assertEqual(result['selected']['adult_lp_response_rise']['date'], '1960-01-08')
            self.assertIn('conversion_rise', result['unobservedCategories'])
            self.assertEqual(len(result['weekly']['a']), 2)


if __name__ == '__main__':
    unittest.main()
